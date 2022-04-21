namespace Coursera_CoderByte
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    class Program
    {
        static void Main(string[] args)
        {
            CourseraContext context = new CourseraContext();

            Console.WriteLine("Enter one or more student PINs separated by coma please...");
            var studentPin = Console.ReadLine().Split(',').ToList();

            Console.WriteLine("Enter start date in format yyyy/mm/dd please...");
            var startDateSplit = Console.ReadLine().Split('/').ToArray();
            DateTime startDate = DateParser(startDateSplit);

            Console.WriteLine("Enter end date in format yyyy/mm/dd please...");
            var endDateSplit = Console.ReadLine().Split('/').ToArray();
            DateTime endDate = DateParser(endDateSplit);

            Console.WriteLine("Enter the minimum amount of credits that students should have please...");
            var minCredits = int.Parse(Console.ReadLine());

            var students = new StudentsService(context);
            var courses = new CoursesService(context);

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("<table>");
            stringBuilder.Append("<tr><th>Student</th><th>Total Credit</th></tr>");

            ReportParser(studentPin, startDate, endDate, minCredits, students, courses, stringBuilder);

            stringBuilder.Append("</table>");
            File.WriteAllText(@"d:\test.html", stringBuilder.ToString());
        }

        private static void ReportParser(List<string> studentPin, DateTime startDate, DateTime endDate, int minCredits, StudentsService students, CoursesService courses, StringBuilder stringBuilder)
        {
            foreach (var pin in studentPin)
            {
                int credits = students.GetTotalCreditsByIdInPeriodOftime(pin, startDate, endDate);

                if (credits > minCredits)
                {
                    var name = students.GetNameById(pin);
                    var fullName = name.FirstName + " " + name.LastName;

                    stringBuilder.Append($"<td>{fullName}</td><td>{credits}</td>");
                    stringBuilder.Append("<tr><th></th><th>Course Name</th><th>Time</th><th>Credit</th><th>Instructor</th></tr>");

                    var studentCourses = courses.GetByStudentPinInPeriodOfTime(pin, startDate, endDate);

                    foreach (var course in studentCourses)
                    {
                        stringBuilder.Append($"<tr><td></td><td>{course.Name}</td><td>{course.Time}</td><td>{course.Credit}</td><td>{course.InstructorName}</td></tr>");
                    }
                }
            }
        }
        private static DateTime DateParser(string[] dateSplit)
        {
            var year = int.Parse(dateSplit[0]);
            var month = int.Parse(dateSplit[1]);
            var day = int.Parse(dateSplit[2]);

            var date = new DateTime(year, month, day);
            return date;
        }
    }

    public interface ICoursesService
    {
        List<string> GetByStudentPin(string pin);

        List<CourseViewModel> GetByStudentPinInPeriodOfTime(string pin, DateTime startDate, DateTime endDate);
    }

    public class CoursesService : ICoursesService
    {
        private readonly CourseraContext context;

        public CoursesService(CourseraContext context)
        {
            this.context = context;
        }

        public List<string> GetByStudentPin(string pin)
        {
            var student = context.Students.FirstOrDefault(x => x.Pin == pin);

            if (student is null)
            {
                throw new NullReferenceException($"Unfortunately we couldn't find any student matching this Pin: {pin}!");
            }

            return this.context.StudentsCoursesXrefs
                           .Where(x => x.StudentPin == pin)
                           .Select(x => x.Course.Name)
                           .ToList();
        }

        public List<CourseViewModel> GetByStudentPinInPeriodOfTime(string pin, DateTime startDate, DateTime endDate)
        {
            var student = context.Students.FirstOrDefault(x => x.Pin == pin);

            if (student is null)
            {
                throw new NullReferenceException($"Unfortunately we couldn't find any student matching this Pin: {pin}!");
            }

            return this.context.StudentsCoursesXrefs
                .Where(x => x.StudentPin == pin
                      && x.CompletionDate >= startDate
                      && x.CompletionDate <= endDate)
                .Select(x => new CourseViewModel
                {
                    Name = x.Course.Name,
                    InstructorName = x.Course.Instructor.FirstName + " " + x.Course.Instructor.LastName,
                    Credit = x.Course.Credit,
                    Time = x.Course.TotalTime,
                })
                .ToList();
        }
    }

    public class CourseViewModel
    {
        public string Name { get; set; }

        public string InstructorName { get; set; }

        public int Time { get; set; }

        public int Credit { get; set; }
    }

    public interface IStudentsService
    {
        int GetTotalCreditsByIdAsync(string pin);

        int GetTotalCreditsByIdInPeriodOftime(string pin, DateTime start, DateTime end);

        StudentViewModel GetNameById(string pin);
    }

    public class StudentsService : IStudentsService
    {
        private readonly CourseraContext context;

        public StudentsService(CourseraContext context)
        {
            this.context = context;
        }

        public StudentViewModel GetNameById(string pin) =>
            this.context.Students.Where(x => x.Pin == pin).Select(x => new StudentViewModel
            {
                FirstName = x.FirstName,
                LastName = x.LastName
            }).FirstOrDefault();

        public int GetTotalCreditsByIdAsync(string pin)
            => this.context.StudentsCoursesXrefs
                .Where(x => x.StudentPin == pin)
                .Sum(x => x.Course.Credit);

        public int GetTotalCreditsByIdInPeriodOftime(string pin, DateTime startDate, DateTime endDate)
        {
            return this.context.StudentsCoursesXrefs
                .Where(x => x.StudentPin == pin
                     && x.CompletionDate >= startDate
                     && x.CompletionDate <= endDate)
                .Sum(x => x.Course.Credit);
        }
    }

    public class StudentViewModel
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
    }

    public partial class StudentsCoursesXref
    {
        public string StudentPin { get; set; }
        public int CourseId { get; set; }
        public DateTime? CompletionDate { get; set; }

        public virtual Course Course { get; set; }
        public virtual Student StudentPinNavigation { get; set; }
    }

    public partial class Student
    {
        public Student()
        {
            StudentsCoursesXrefs = new HashSet<StudentsCoursesXref>();
        }

        public string Pin { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime TimeCreated { get; set; }

        public virtual ICollection<StudentsCoursesXref> StudentsCoursesXrefs { get; set; }
    }

    public partial class Instructor
    {
        public Instructor()
        {
            Courses = new HashSet<Course>();
        }

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime TimeCreated { get; set; }

        public virtual ICollection<Course> Courses { get; set; }
    }

    public partial class Course
    {
        public Course()
        {
            StudentsCoursesXrefs = new HashSet<StudentsCoursesXref>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int InstructorId { get; set; }
        public byte TotalTime { get; set; }
        public byte Credit { get; set; }
        public DateTime TimeCreated { get; set; }

        public virtual Instructor Instructor { get; set; }
        public virtual ICollection<StudentsCoursesXref> StudentsCoursesXrefs { get; set; }
    }

    public partial class CourseraContext : DbContext
    {
        public CourseraContext()
        {
        }

        public CourseraContext(DbContextOptions<CourseraContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Instructor> Instructors { get; set; }
        public virtual DbSet<Student> Students { get; set; }
        public virtual DbSet<StudentsCoursesXref> StudentsCoursesXrefs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=coursera;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Cyrillic_General_CI_AS");

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("courses");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Credit).HasColumnName("credit");

                entity.Property(e => e.InstructorId).HasColumnName("instructor_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnName("name");

                entity.Property(e => e.TimeCreated)
                    .HasColumnType("datetime")
                    .HasColumnName("time_created")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.TotalTime).HasColumnName("total_time");

                entity.HasOne(d => d.Instructor)
                    .WithMany(p => p.Courses)
                    .HasForeignKey(d => d.InstructorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_courses_instructors");
            });

            modelBuilder.Entity<Instructor>(entity =>
            {
                entity.ToTable("instructors");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("first_name");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("last_name");

                entity.Property(e => e.TimeCreated)
                    .HasColumnType("datetime")
                    .HasColumnName("time_created")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Pin);

                entity.ToTable("students");

                entity.Property(e => e.Pin)
                    .HasMaxLength(10)
                    .HasColumnName("pin")
                    .IsFixedLength(true);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("first_name");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("last_name");

                entity.Property(e => e.TimeCreated)
                    .HasColumnType("datetime")
                    .HasColumnName("time_created")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<StudentsCoursesXref>(entity =>
            {
                entity.HasKey(e => new { e.StudentPin, e.CourseId });

                entity.ToTable("students_courses_xref");

                entity.Property(e => e.StudentPin)
                    .HasMaxLength(10)
                    .HasColumnName("student_pin")
                    .IsFixedLength(true);

                entity.Property(e => e.CourseId).HasColumnName("course_id");

                entity.Property(e => e.CompletionDate)
                    .HasColumnType("date")
                    .HasColumnName("completion_date");

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.StudentsCoursesXrefs)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_students_courses_xref_courses");

                entity.HasOne(d => d.StudentPinNavigation)
                    .WithMany(p => p.StudentsCoursesXrefs)
                    .HasForeignKey(d => d.StudentPin)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_students_courses_xref_students");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
