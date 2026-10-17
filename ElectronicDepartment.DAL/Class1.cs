using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ElectronicDepartment.DAL
{
    // 1. Сутність викладача
    public class TeacherEntity
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string AcademicDegree { get; set; }
    }

    // 2. Сутність академічної групи
    public class GroupEntity
    {
        public int Id { get; set; }
        public string Code { get; set; }
    }

    // 3. Сутність студента
    public class StudentEntity
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int GroupId { get; set; }
        public string StudentCardNumber { get; set; }
    }

    // 4. Сутність навчальної дисципліни
    public class DisciplineEntity
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int TeacherId { get; set; }
    }

    // 5. Сутність оцінки студента
    public class GradeEntity
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int DisciplineId { get; set; }
        public int Points { get; set; }
        public string GradeType { get; set; }
    }

    // Контекст бази даних Entity Framework Code First
    public class DepartmentContext : DbContext
    {
        public DbSet<TeacherEntity> Teachers { get; set; }
        public DbSet<GroupEntity> Groups { get; set; }
        public DbSet<StudentEntity> Students { get; set; }
        public DbSet<DisciplineEntity> Disciplines { get; set; }
        public DbSet<GradeEntity> Grades { get; set; }

        public DepartmentContext(DbContextOptions<DepartmentContext> options) : base(options) { }
    }

    // Інтерфейс Generic-репозиторію
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        T GetById(int id);
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
    }

    // Реалізація Generic-репозиторію
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly DepartmentContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DepartmentContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IEnumerable<T> GetAll() => _dbSet.ToList();
        public T GetById(int id) => _dbSet.Find(id);
        public void Add(T entity) => _dbSet.Add(entity);
        public void Update(T entity) => _dbSet.Update(entity);
        public void Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null) _dbSet.Remove(entity);
        }
    }

    // Інтерфейс Unit of Work
    public interface IUnitOfWork : IDisposable
    {
        IRepository<TeacherEntity> Teachers { get; }
        IRepository<GroupEntity> Groups { get; }
        IRepository<StudentEntity> Students { get; }
        IRepository<DisciplineEntity> Disciplines { get; }
        IRepository<GradeEntity> Grades { get; }
        void Save();
    }

    // Реалізація Unit of Work
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DepartmentContext _context;
        private IRepository<TeacherEntity> _teachers;
        private IRepository<GroupEntity> _groups;
        private IRepository<StudentEntity> _students;
        private IRepository<DisciplineEntity> _disciplines;
        private IRepository<GradeEntity> _grades;

        public UnitOfWork(DepartmentContext context)
        {
            _context = context;
        }

        public IRepository<TeacherEntity> Teachers => _teachers ??= new Repository<TeacherEntity>(_context);
        public IRepository<GroupEntity> Groups => _groups ??= new Repository<GroupEntity>(_context);
        public IRepository<StudentEntity> Students => _students ??= new Repository<StudentEntity>(_context);
        public IRepository<DisciplineEntity> Disciplines => _disciplines ??= new Repository<DisciplineEntity>(_context);
        public IRepository<GradeEntity> Grades => _grades ??= new Repository<GradeEntity>(_context);

        public void Save() => _context.SaveChanges();
        public void Dispose() => _context.Dispose();
    }
}