using System;
using System.Collections.Generic;
using System.Linq;
using ElectronicDepartment.DAL;

namespace ElectronicDepartment.BLL
{
    // DTO-модель для передачі студента на клієнт (Swagger/веб-інтерфейс)
    public class StudentDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string GroupCode { get; set; }
        public string StudentCardNumber { get; set; }
    }

    // DTO для перегляду оцінки з розрахованою літерою ECTS
    public class GradeDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string DisciplineTitle { get; set; }
        public int Points { get; set; }
        public string GradeType { get; set; }
        public string EctsGrade { get; set; }
    }

    // Модель вхідних даних для реєстрації нового студента
    public class StudentRequestModel
    {
        public string FullName { get; set; }
        public int GroupId { get; set; }
        public string StudentCardNumber { get; set; }
    }

    // Модель для внесення балів викладачем
    public class AddGradeRequestModel
    {
        public int StudentId { get; set; }
        public int DisciplineId { get; set; }
        public int Points { get; set; }
        public string GradeType { get; set; }
    }

    // Контракт сервісу бізнес-логіки кафедри
    public interface IDepartmentService
    {
        void InitializeDefaultData();
        IEnumerable<StudentDto> GetAllStudents(string groupFilter = null);
        StudentDto GetStudentById(int id);
        StudentDto CreateStudent(StudentRequestModel model);
        GradeDto AddGrade(AddGradeRequestModel model);
        IEnumerable<GradeDto> GetGradesByStudentId(int studentId);
        bool DeleteStudent(int id);
    }

    public class DepartmentService : IDepartmentService
    {
        // Доступ до сховища через координатор Unit of Work (слабка зв'язаність)
        private readonly IUnitOfWork _uow;

        public DepartmentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // Заповнення бази тестовими даними при першому запуску
        public void InitializeDefaultData()
        {
            if (!_uow.Groups.GetAll().Any())
            {
                // Створюємо початкову групу та викладача
                var group = new GroupEntity { Code = "Бз121-1" };
                _uow.Groups.Add(group);

                var teacher = new TeacherEntity
                {
                    FullName = "Дишлевий О.П.",
                    AcademicDegree = "Доцент"
                };
                _uow.Teachers.Add(teacher);
                _uow.Save();

                // Прив'язуємо предмет до створеного викладача
                var disc = new DisciplineEntity
                {
                    Title = "Архітектура та проектування ПЗ",
                    TeacherId = teacher.Id
                };
                _uow.Disciplines.Add(disc);

                // Додаємо перших студентів до групи
                var st1 = new StudentEntity
                {
                    FullName = "Анастасія Терехова",
                    GroupId = group.Id,
                    StudentCardNumber = "KB12345699"
                };
                var st2 = new StudentEntity
                {
                    FullName = "Іван Бондаренко",
                    GroupId = group.Id,
                    StudentCardNumber = "KB12345610"
                };
                _uow.Students.Add(st1);
                _uow.Students.Add(st2);
                _uow.Save();

                // Початкові оцінки за виконану лабораторну
                _uow.Grades.Add(new GradeEntity
                {
                    StudentId = st1.Id,
                    DisciplineId = disc.Id,
                    Points = 95,
                    GradeType = "Лабораторна 2.4"
                });
                _uow.Grades.Add(new GradeEntity
                {
                    StudentId = st2.Id,
                    DisciplineId = disc.Id,
                    Points = 78,
                    GradeType = "Лабораторна 2.4"
                });

                _uow.Save();
            }
        }

        // Отримання списку всіх студентів із необов'язковою фільтрацією за групою
        public IEnumerable<StudentDto> GetAllStudents(string groupFilter = null)
        {
            var groups = _uow.Groups.GetAll().ToDictionary(g => g.Id, g => g.Code);
            var students = _uow.Students.GetAll();

            // Фільтруємо, якщо передано назву групи
            if (!string.IsNullOrWhiteSpace(groupFilter))
            {
                students = students.Where(s => groups.ContainsKey(s.GroupId) &&
                                               groups[s.GroupId].Equals(groupFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Мапінг сутностей DAL у публічні моделі DTO
            return students.Select(s => new StudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                GroupCode = groups.ContainsKey(s.GroupId) ? groups[s.GroupId] : "Невідомо",
                StudentCardNumber = s.StudentCardNumber
            }).ToList();
        }

        // Пошук інформації про студента за його Id
        public StudentDto GetStudentById(int id)
        {
            var s = _uow.Students.GetById(id);
            if (s == null) return null;

            var group = _uow.Groups.GetById(s.GroupId);
            return new StudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                GroupCode = group?.Code ?? "Невідомо",
                StudentCardNumber = s.StudentCardNumber
            };
        }

        // Додавання нового студента з валідацією обов'язкових полів
        public StudentDto CreateStudent(StudentRequestModel model)
        {
            // Бізнес-правило: ПІБ не може бути порожнім
            if (string.IsNullOrWhiteSpace(model.FullName))
                throw new ArgumentException("ПІБ студента не може бути порожнім");

            var entity = new StudentEntity
            {
                FullName = model.FullName,
                GroupId = model.GroupId,
                StudentCardNumber = model.StudentCardNumber
            };

            _uow.Students.Add(entity);
            _uow.Save();

            return GetStudentById(entity.Id);
        }

        // Виставлення оцінки викладачем та розрахунок шкали ECTS
        public GradeDto AddGrade(AddGradeRequestModel model)
        {
            // Бізнес-правило: перевіряємо допустимий діапазон балів (0-100)
            if (model.Points < 0 || model.Points > 100)
                throw new ArgumentOutOfRangeException(nameof(model.Points), "Кількість балів повинна бути в діапазоні від 0 до 100");

            var student = _uow.Students.GetById(model.StudentId);
            if (student == null) throw new InvalidOperationException("Студента не знайдено");

            var discipline = _uow.Disciplines.GetById(model.DisciplineId);
            if (discipline == null) throw new InvalidOperationException("Дисципліну не знайдено");

            var grade = new GradeEntity
            {
                StudentId = model.StudentId,
                DisciplineId = model.DisciplineId,
                Points = model.Points,
                GradeType = model.GradeType
            };

            _uow.Grades.Add(grade);
            _uow.Save();

            return new GradeDto
            {
                Id = grade.Id,
                StudentName = student.FullName,
                DisciplineTitle = discipline.Title,
                Points = grade.Points,
                GradeType = grade.GradeType,
                EctsGrade = CalculateEcts(grade.Points) // Автоматичний розрахунок оцінки
            };
        }

        // Отримання всіх оцінок конкретного студента
        public IEnumerable<GradeDto> GetGradesByStudentId(int studentId)
        {
            var student = _uow.Students.GetById(studentId);
            if (student == null) return Enumerable.Empty<GradeDto>();

            var disciplines = _uow.Disciplines.GetAll().ToDictionary(d => d.Id, d => d.Title);
            var grades = _uow.Grades.GetAll().Where(g => g.StudentId == studentId);

            return grades.Select(g => new GradeDto
            {
                Id = g.Id,
                StudentName = student.FullName,
                DisciplineTitle = disciplines.ContainsKey(g.DisciplineId) ? disciplines[g.DisciplineId] : "Невідомо",
                Points = g.Points,
                GradeType = g.GradeType,
                EctsGrade = CalculateEcts(g.Points)
            }).ToList();
        }

        // Видалення картки студента
        public bool DeleteStudent(int id)
        {
            var student = _uow.Students.GetById(id);
            if (student == null) return false;

            _uow.Students.Delete(id);
            _uow.Save();
            return true;
        }

        // Переведення 100-бальної шкали у європейську систему ECTS
        private static string CalculateEcts(int points) => points switch
        {
            >= 90 => "A",
            >= 82 => "B",
            >= 75 => "C",
            >= 67 => "D",
            >= 60 => "E",
            _ => "FX"
        };
    }
}