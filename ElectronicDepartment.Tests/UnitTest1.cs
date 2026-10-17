using System;
using System.Collections.Generic;
using ElectronicDepartment.BLL;
using ElectronicDepartment.DAL;
using Moq;
using Xunit;

namespace ElectronicDepartment.Tests
{
    public class DepartmentServiceTests
    {
        [Fact]
        public void AddGrade_ValidPoints_ReturnsGradeDtoWithCalculatedEcts()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            var mockStudentRepo = new Mock<IRepository<StudentEntity>>();
            var mockDisciplineRepo = new Mock<IRepository<DisciplineEntity>>();
            var mockGradeRepo = new Mock<IRepository<GradeEntity>>();

            mockStudentRepo.Setup(r => r.GetById(1)).Returns(new StudentEntity { Id = 1, FullName = "Анастасія Терехова" });
            mockDisciplineRepo.Setup(r => r.GetById(1)).Returns(new DisciplineEntity { Id = 1, Title = "Архітектура ПЗ" });
            mockGradeRepo.Setup(r => r.GetAll()).Returns(new List<GradeEntity>());

            mockUow.Setup(u => u.Students).Returns(mockStudentRepo.Object);
            mockUow.Setup(u => u.Disciplines).Returns(mockDisciplineRepo.Object);
            mockUow.Setup(u => u.Grades).Returns(mockGradeRepo.Object);

            var service = new DepartmentService(mockUow.Object);

            var request = new AddGradeRequestModel
            {
                StudentId = 1,
                DisciplineId = 1,
                Points = 95,
                GradeType = "Іспит"
            };

            // Act
            var result = service.AddGrade(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("A", result.EctsGrade);
            Assert.Equal(95, result.Points);
            mockGradeRepo.Verify(r => r.Add(It.IsAny<GradeEntity>()), Times.Once);
            mockUow.Verify(u => u.Save(), Times.Once);
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(105)]
        public void AddGrade_InvalidPoints_ThrowsArgumentOutOfRangeException(int invalidPoints)
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            var service = new DepartmentService(mockUow.Object);

            var request = new AddGradeRequestModel
            {
                StudentId = 1,
                DisciplineId = 1,
                Points = invalidPoints,
                GradeType = "Тест"
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => service.AddGrade(request));
        }

        [Fact]
        public void CreateStudent_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var mockUow = new Mock<IUnitOfWork>();
            var service = new DepartmentService(mockUow.Object);

            var request = new StudentRequestModel
            {
                FullName = "   ",
                GroupId = 1,
                StudentCardNumber = "12345"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.CreateStudent(request));
        }
    }
}