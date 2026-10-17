using ElectronicDepartment.BLL;
using ElectronicDepartment.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Реєстрація DbContext та Unit of Work
builder.Services.AddDbContext<DepartmentContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ElectronicDepartmentDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();

var app = builder.Build();

// Наповнення тестовими даними під час першого старту
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DepartmentContext>();
    context.Database.EnsureCreated();

    var service = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
    service.InitializeDefaultData();
}

// 1. Інтерактивна сторінка для перевірки в браузері
app.MapGet("/", () => Results.Content(
    "<html><body style='font-family:Segoe UI, sans-serif; padding:40px; background-color:#f8f9fa;'>" +
    "<h2>Електронна кафедра (Курсова робота - Варіант 10)</h2>" +
    "<p>Оберіть посилання для перевірки роботи Web API:</p>" +
    "<ul>" +
    "<li><a href='/api/students'><b>GET /api/students</b> (Отримати список усіх студентів)</a></li>" +
    "<li><a href='/api/students?groupFilter=Бз121-1'><b>GET /api/students?groupFilter=...</b> (Фільтрація студентів за групою)</a></li>" +
    "<li><a href='/api/grades/student/1'><b>GET /api/grades/student/1</b> (Отримати відомість оцінок студентки #1)</a></li>" +
    "<li><a href='/api/grades/add-demo'><b>POST /api/grades/add-demo</b> (Викладач виставляє оцінку 98 балів за Курсову)</a></li>" +
    "<li><a href='/api/students/create-demo'><b>POST /api/students/create-demo</b> (Додати нового студента)</a></li>" +
    "</ul>" +
    "</body></html>", "text/html; charset=utf-8"));

// 2. GET: список студентів
app.MapGet("/api/students", (string? groupFilter, IDepartmentService service) =>
    Results.Ok(service.GetAllStudents(groupFilter)));

// 3. GET: оцінки студента
app.MapGet("/api/grades/student/{id:int}", (int id, IDepartmentService service) =>
    Results.Ok(service.GetGradesByStudentId(id)));

// 4. POST: виставлення оцінки
app.MapGet("/api/grades/add-demo", (IDepartmentService service) =>
{
    var result = service.AddGrade(new AddGradeRequestModel
    {
        StudentId = 1,
        DisciplineId = 1,
        Points = 98,
        GradeType = "Курсова робота"
    });
    return Results.Ok(new { Message = "Викладач успішно виставив оцінку!", Grade = result });
});

// 5. POST: додавання студента
app.MapGet("/api/students/create-demo", (IDepartmentService service) =>
{
    var result = service.CreateStudent(new StudentRequestModel
    {
        FullName = "Олександр Коваленко",
        GroupId = 1,
        StudentCardNumber = "KB12345695"
    });
    return Results.Ok(new { Message = "Студента успішно додано до бази!", Student = result });
});

app.Run();