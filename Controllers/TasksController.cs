using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMvcPostgresApp.Data;
using MyMvcPostgresApp.Models;
using MyMvcPostgresApp.ViewModels;
using System.Security.Claims;

namespace MyMvcPostgresApp.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TasksController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Tasks/Board/5
        public async Task<IActionResult> Board(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Sprawdź czy użytkownik jest członkiem projektu
            var member = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.ProjectId == id && pm.UserId == userId);

            if (member == null)
            {
                TempData["Error"] = "Nie masz dostępu do tego projektu";
                return RedirectToAction("Index", "MyProjects");
            }

            // Pobierz wszystkie zadania projektu
            var tasks = await _context.ProjectTasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Where(t => t.ProjectId == id)
                .OrderBy(t => t.Priority == "Critical" ? 0 : t.Priority == "High" ? 1 : t.Priority == "Medium" ? 2 : 3)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            // Pobierz członków zespołu
            var teamMembers = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == id)
                .Select(pm => pm.User)
                .ToListAsync();

            var viewModel = new TaskBoardViewModel
            {
                ProjectId = id,
                ProjectName = member.Project.Name,
                IsLead = member.Role == "Lead",
                ToDoTasks = tasks.Where(t => t.Status == "ToDo").ToList(),
                InProgressTasks = tasks.Where(t => t.Status == "InProgress").ToList(),
                InReviewTasks = tasks.Where(t => t.Status == "InReview").ToList(),
                DoneTasks = tasks.Where(t => t.Status == "Done").ToList(),
                BlockedTasks = tasks.Where(t => t.Status == "Blocked").ToList(),
                TeamMembers = teamMembers,
                StatusCounts = new Dictionary<string, int>
                {
                    ["ToDo"] = tasks.Count(t => t.Status == "ToDo"),
                    ["InProgress"] = tasks.Count(t => t.Status == "InProgress"),
                    ["InReview"] = tasks.Count(t => t.Status == "InReview"),
                    ["Done"] = tasks.Count(t => t.Status == "Done"),
                    ["Blocked"] = tasks.Count(t => t.Status == "Blocked")
                }
            };

            return View(viewModel);
        }

        // GET: Tasks/Create/5
        [HttpGet]
        public async Task<IActionResult> Create(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Sprawdź czy użytkownik jest liderem projektu
            var member = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.ProjectId == id && pm.UserId == userId);

            if (member == null || member.Role != "Lead")
            {
                TempData["Error"] = "Tylko liderzy projektów mogą tworzyć zadania";
                return RedirectToAction("Board", new { id });
            }

            // Pobierz członków zespołu
            var teamMembers = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == id)
                .Select(pm => pm.User)
                .ToListAsync();

            ViewBag.TeamMembers = teamMembers;
            ViewBag.ProjectId = id;
            ViewBag.ProjectName = member.Project.Name;

            return View(new CreateTaskViewModel { ProjectId = id });
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Sprawdź czy użytkownik jest liderem projektu
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == model.ProjectId && pm.UserId == userId && pm.Role == "Lead");

            if (member == null)
            {
                TempData["Error"] = "Tylko liderzy projektów mogą tworzyć zadania";
                return RedirectToAction("Board", new { id = model.ProjectId });
            }

            if (!ModelState.IsValid)
            {
                var teamMembers = await _context.ProjectMembers
                    .Include(pm => pm.User)
                    .Where(pm => pm.ProjectId == model.ProjectId)
                    .Select(pm => pm.User)
                    .ToListAsync();

                ViewBag.TeamMembers = teamMembers;
                ViewBag.ProjectId = model.ProjectId;
                return View(model);
            }

            var task = new ProjectTask
            {
                ProjectId = model.ProjectId,
                Title = model.Title,
                Description = model.Description,
                AssignedToUserId = model.AssignedToUserId,
                Priority = model.Priority,
                DueDate = model.DueDate.HasValue ? DateTime.SpecifyKind(model.DueDate.Value, DateTimeKind.Utc) : null, // FIX
                EstimatedHours = model.EstimatedHours,
                Tags = model.Tags,
                CreatedByUserId = userId,
                Status = "ToDo",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProjectTasks.Add(task);

            // Dodaj wpis do historii
            var history = new TaskHistory
            {
                Task = task,
                UserId = userId,
                Action = "Created",
                Details = $"Zadanie utworzone",
                CreatedAt = DateTime.UtcNow
            };
            _context.TaskHistory.Add(history);

            // Jeśli zadanie jest przypisane, wyślij powiadomienie
            if (model.AssignedToUserId.HasValue)
            {
                var assignedUser = await _context.Users.FindAsync(model.AssignedToUserId.Value);
                var project = await _context.Projects.FindAsync(model.ProjectId);

                var notification = new Notification
                {
                    UserId = model.AssignedToUserId.Value,
                    Message = $"Przypisano Ci nowe zadanie '{model.Title}' w projekcie '{project?.Name}'",
                    Type = "Info",
                    ProjectId = model.ProjectId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Zadanie zostało utworzone pomyślnie";
            return RedirectToAction("Board", new { id = model.ProjectId });
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.UploadedByUser)
                .Include(t => t.History).ThenInclude(h => h.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                TempData["Error"] = "Nie znaleziono zadania";
                return RedirectToAction("Index", "MyProjects");
            }

            // Sprawdź czy użytkownik ma dostęp do projektu
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

            if (member == null)
            {
                TempData["Error"] = "Nie masz dostępu do tego zadania";
                return RedirectToAction("Index", "MyProjects");
            }

            // Pobierz dostępnych członków zespołu
            var availableAssignees = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == task.ProjectId)
                .Select(pm => pm.User)
                .ToListAsync();

            var viewModel = new TaskDetailsViewModel
            {
                Task = task,
                CanEdit = member.Role == "Lead" || task.AssignedToUserId == userId || task.CreatedByUserId == userId,
                CanDelete = member.Role == "Lead" || task.CreatedByUserId == userId,
                CanComment = true,
                AvailableAssignees = availableAssignees,
                Comments = task.Comments.OrderByDescending(c => c.CreatedAt).ToList(),
                Attachments = task.Attachments.OrderByDescending(a => a.UploadedAt).ToList(),
                History = task.History.OrderByDescending(h => h.CreatedAt).ToList()
            };

            return View(viewModel);
        }

        // GET: Tasks/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                TempData["Error"] = "Nie znaleziono zadania";
                return RedirectToAction("Index", "MyProjects");
            }

            // Sprawdź uprawnienia
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

            if (member == null)
            {
                TempData["Error"] = "Nie masz dostępu do tego zadania";
                return RedirectToAction("Board", new { id = task.ProjectId });
            }

            bool canEdit = member.Role == "Lead" || task.AssignedToUserId == userId || task.CreatedByUserId == userId;
            if (!canEdit)
            {
                TempData["Error"] = "Nie masz uprawnień do edycji tego zadania";
                return RedirectToAction("Details", new { id });
            }

            // Pobierz członków zespołu
            var teamMembers = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == task.ProjectId)
                .Select(pm => pm.User)
                .ToListAsync();

            ViewBag.TeamMembers = teamMembers;
            ViewBag.ProjectName = task.Project.Name;

            var model = new EditTaskViewModel
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                AssignedToUserId = task.AssignedToUserId,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                ProgressPercentage = task.ProgressPercentage,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                Tags = task.Tags
            };

            return View(model);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTaskViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var task = await _context.ProjectTasks.FindAsync(model.Id);
            if (task == null)
            {
                TempData["Error"] = "Nie znaleziono zadania";
                return RedirectToAction("Index", "MyProjects");
            }

            // Sprawdź uprawnienia
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

            bool canEdit = member?.Role == "Lead" || task.AssignedToUserId == userId || task.CreatedByUserId == userId;
            if (!canEdit)
            {
                TempData["Error"] = "Nie masz uprawnień do edycji tego zadania";
                return RedirectToAction("Details", new { id = model.Id });
            }

            if (!ModelState.IsValid)
            {
                var teamMembers = await _context.ProjectMembers
                    .Include(pm => pm.User)
                    .Where(pm => pm.ProjectId == task.ProjectId)
                    .Select(pm => pm.User)
                    .ToListAsync();

                ViewBag.TeamMembers = teamMembers;
                return View(model);
            }

            // Śledź zmiany
            var changes = new List<string>();

            if (task.Title != model.Title)
            {
                changes.Add($"Tytuł: '{task.Title}' → '{model.Title}'");
                task.Title = model.Title;
            }

            if (task.Description != model.Description)
            {
                task.Description = model.Description;
                changes.Add("Zaktualizowano opis");
            }

            if (task.Status != model.Status)
            {
                var history = new TaskHistory
                {
                    TaskId = task.Id,
                    UserId = userId,
                    Action = "StatusChanged",
                    Details = "Status zmieniony",
                    OldValue = task.Status,
                    NewValue = model.Status,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TaskHistory.Add(history);

                task.Status = model.Status;

                if (model.Status == "InProgress" && !task.StartedAt.HasValue)
                {
                    task.StartedAt = DateTime.UtcNow;
                }
                else if (model.Status == "Done" && !task.CompletedAt.HasValue)
                {
                    task.CompletedAt = DateTime.UtcNow;
                    task.ProgressPercentage = 100;
                }
            }

            if (task.Priority != model.Priority)
            {
                changes.Add($"Priorytet: {task.Priority} → {model.Priority}");
                task.Priority = model.Priority;
            }

            if (task.AssignedToUserId != model.AssignedToUserId)
            {
                var oldAssignee = task.AssignedToUserId.HasValue ?
                    (await _context.Users.FindAsync(task.AssignedToUserId.Value))?.Login : "Brak";
                var newAssignee = model.AssignedToUserId.HasValue ?
                    (await _context.Users.FindAsync(model.AssignedToUserId.Value))?.Login : "Brak";

                var history = new TaskHistory
                {
                    TaskId = task.Id,
                    UserId = userId,
                    Action = "AssignedTo",
                    Details = "Przypisanie zmienione",
                    OldValue = oldAssignee,
                    NewValue = newAssignee,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TaskHistory.Add(history);

                // Powiadomienie dla nowego przypisanego użytkownika
                if (model.AssignedToUserId.HasValue && model.AssignedToUserId != task.AssignedToUserId)
                {
                    var project = await _context.Projects.FindAsync(task.ProjectId);
                    var notification = new Notification
                    {
                        UserId = model.AssignedToUserId.Value,
                        Message = $"Przypisano Ci zadanie '{task.Title}' w projekcie '{project?.Name}'",
                        Type = "Info",
                        ProjectId = task.ProjectId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }

                task.AssignedToUserId = model.AssignedToUserId;
            }

            task.DueDate = model.DueDate;
            task.ProgressPercentage = model.ProgressPercentage;
            task.EstimatedHours = model.EstimatedHours;
            task.ActualHours = model.ActualHours;
            task.Tags = model.Tags;
            task.UpdatedAt = DateTime.UtcNow;

            if (changes.Any())
            {
                var history = new TaskHistory
                {
                    TaskId = task.Id,
                    UserId = userId,
                    Action = "Updated",
                    Details = string.Join(", ", changes),
                    CreatedAt = DateTime.UtcNow
                };
                _context.TaskHistory.Add(history);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Zadanie zostało zaktualizowane";
            return RedirectToAction("Details", new { id = task.Id });
        }

        // POST: Tasks/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddTaskCommentViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var task = await _context.ProjectTasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == model.TaskId);

            if (task == null)
            {
                TempData["Error"] = "Nie znaleziono zadania";
                return RedirectToAction("Index", "MyProjects");
            }

            // Sprawdź dostęp
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

            if (member == null)
            {
                TempData["Error"] = "Nie masz dostępu do tego zadania";
                return RedirectToAction("Index", "MyProjects");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Nieprawidłowe dane komentarza";
                return RedirectToAction("Details", new { id = model.TaskId });
            }

            var comment = new TaskComment
            {
                TaskId = model.TaskId,
                UserId = userId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            }; _context.TaskComments.Add(comment);        // Dodaj wpis do historii
            var history = new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Action = "CommentAdded",
                Details = "Dodano komentarz",
                CreatedAt = DateTime.UtcNow
            };
            _context.TaskHistory.Add(history);        // Powiadom osobę przypisaną do zadania (jeśli to nie ona dodała komentarz)
            if (task.AssignedToUserId.HasValue && task.AssignedToUserId != userId)
            {
                var notification = new Notification
                {
                    UserId = task.AssignedToUserId.Value,
                    Message = $"Nowy komentarz w zadaniu '{task.Title}'",
                    Type = "Info",
                    ProjectId = task.ProjectId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }        // Powiadom twórcę zadania (jeśli to nie on dodał komentarz)
            if (task.CreatedByUserId != userId && task.CreatedByUserId != task.AssignedToUserId)
            {
                var notification = new Notification
                {
                    UserId = task.CreatedByUserId,
                    Message = $"Nowy komentarz w zadaniu '{task.Title}'",
                    Type = "Info",
                    ProjectId = task.ProjectId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }
            await _context.SaveChangesAsync(); TempData["Success"] = "Komentarz został dodany";
            return RedirectToAction("Details", new { id = model.TaskId });
        }    // POST: Tasks/UploadAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Nie wybrano pliku";
                return RedirectToAction("Details", new { id = taskId });
            }        // Sprawdź rozmiar (max 10 MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "Plik jest za duży. Maksymalny rozmiar to 10 MB";
                return RedirectToAction("Details", new { id = taskId });
            }
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
            {
                TempData["Error"] = "Nie znaleziono zadania";
                return RedirectToAction("Index", "MyProjects");
            }        // Sprawdź dostęp
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId); if (member == null)
            {
                TempData["Error"] = "Nie masz dostępu do tego zadania";
                return RedirectToAction("Index", "MyProjects");
            }        // Utwórz folder uploads jeśli nie istnieje
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "tasks");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }        // Wygeneruj unikalną nazwę pliku
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);        // Zapisz plik
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }        // Zapisz informacje o załączniku w bazie
            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                UploadedByUserId = userId,
                FileName = file.FileName,
                FilePath = $"/uploads/tasks/{uniqueFileName}",
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            }; _context.TaskAttachments.Add(attachment);        // Dodaj wpis do historii
            var history = new TaskHistory
            {
                TaskId = taskId,
                UserId = userId,
                Action = "AttachmentAdded",
                Details = $"Dodano załącznik: {file.FileName}",
                CreatedAt = DateTime.UtcNow
            };
            _context.TaskHistory.Add(history); await _context.SaveChangesAsync(); TempData["Success"] = "Załącznik został dodany";
            return RedirectToAction("Details", new { id = taskId });
        }    // POST: Tasks/DeleteAttachment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var attachment = await _context.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == id); if (attachment == null)
            {
                TempData["Error"] = "Nie znaleziono załącznika";
                return RedirectToAction("Index", "MyProjects");
            }        // Sprawdź uprawnienia
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == attachment.Task.ProjectId && pm.UserId == userId); bool canDelete = member?.Role == "Lead" || attachment.UploadedByUserId == userId;
            if (!canDelete)
            {
                TempData["Error"] = "Nie masz uprawnień do usunięcia tego załącznika";
                return RedirectToAction("Details", new { id = attachment.TaskId });
            }        // Usuń plik fizyczny
            var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }        // Usuń z bazy
            _context.TaskAttachments.Remove(attachment);        // Dodaj wpis do historii
            var history = new TaskHistory
            {
                TaskId = attachment.TaskId,
                UserId = userId,
                Action = "AttachmentDeleted",
                Details = $"Usunięto załącznik: {attachment.FileName}",
                CreatedAt = DateTime.UtcNow
            };
            _context.TaskHistory.Add(history); await _context.SaveChangesAsync(); TempData["Success"] = "Załącznik został usunięty";
            return RedirectToAction("Details", new { id = attachment.TaskId });
        }    // POST: Tasks/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var task = await _context.ProjectTasks
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id); if (task == null)
            {
                TempData["Error"] = "Nie znaleziono zadania";
                return RedirectToAction("Index", "MyProjects");
            }        // Sprawdź uprawnienia
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId); bool canDelete = member?.Role == "Lead" || task.CreatedByUserId == userId;
            if (!canDelete)
            {
                TempData["Error"] = "Nie masz uprawnień do usunięcia tego zadania";
                return RedirectToAction("Details", new { id });
            }
            var projectId = task.ProjectId;        // Usuń wszystkie fizyczne pliki załączników
            foreach (var attachment in task.Attachments)
            {
                var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }        // Usuń zadanie (cascade delete zajmie się powiązanymi rekordami)
            _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync(); TempData["Success"] = "Zadanie zostało usunięte";
            return RedirectToAction("Board", new { id = projectId });
        }    // POST: Tasks/QuickUpdateStatus
        [HttpPost]
        public async Task<IActionResult> QuickUpdateStatus(int id, string status)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Nieautoryzowany dostęp" });
            }

            var task = await _context.ProjectTasks.FindAsync(id);
            if (task == null)
            {
                return Json(new { success = false, message = "Nie znaleziono zadania" });
            }

            // Sprawdź dostęp
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

            if (member == null)
            {
                return Json(new { success = false, message = "Brak dostępu do tego zadania" });
            }

            // Sprawdź czy użytkownik może edytować
            bool canEdit = member.Role == "Lead" || task.AssignedToUserId == userId || task.CreatedByUserId == userId;
            if (!canEdit)
            {
                return Json(new { success = false, message = "Nie masz uprawnień do zmiany statusu tego zadania" });
            }

            var oldStatus = task.Status;
            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;

            if (status == "InProgress" && !task.StartedAt.HasValue)
            {
                task.StartedAt = DateTime.UtcNow;
            }
            else if (status == "Done" && !task.CompletedAt.HasValue)
            {
                task.CompletedAt = DateTime.UtcNow;
                task.ProgressPercentage = 100;
            }

            var history = new TaskHistory
            {
                TaskId = task.Id,
                UserId = userId,
                Action = "StatusChanged",
                Details = "Status zmieniony",
                OldValue = oldStatus,
                NewValue = status,
                CreatedAt = DateTime.UtcNow
            };
            _context.TaskHistory.Add(history);

            await _context.SaveChangesAsync();

            string statusName = status switch
            {
                "InProgress" => "W trakcie",
                "InReview" => "Do sprawdzenia",
                "Done" => "Ukończone",
                "Blocked" => "Zablokowane",
                "ToDo" => "Do zrobienia",
                _ => status
            };

            return Json(new
            {
                success = true,
                message = $"Status zmieniony na: {statusName}",
                newStatus = status
            });
        }
        public async Task<IActionResult> MyTasks()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var myTasks = await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Where(t => t.AssignedToUserId == userId)
                .OrderBy(t => t.Status == "Blocked" ? 0 : t.Status == "InProgress" ? 1 : t.Status == "InReview" ? 2 : t.Status == "ToDo" ? 3 : 4)
                .ThenBy(t => t.Priority == "Critical" ? 0 : t.Priority == "High" ? 1 : t.Priority == "Medium" ? 2 : 3)
                .ThenBy(t => t.DueDate)
                .ToListAsync(); return View(myTasks);
        }
    }
}