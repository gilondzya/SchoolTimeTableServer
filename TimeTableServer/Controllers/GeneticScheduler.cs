using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTableServer.Models;

namespace TimeTableServer.Controllers
{
    public static class GeneticScheduler
    {
        public static Plan createdPlan;

        public static void GeneticSchedulerMain(List<Plan> plans)
        {
            var solver = new Solver();//создаем решатель

            solver.FitnessFunctions.Add(FitnessFunctions.Windows);//будем штрафовать за окна
            solver.FitnessFunctions.Add(FitnessFunctions.LateLesson);//будем штрафовать за поздние пары

            createdPlan = solver.Solve(plans);//находим лучший план
        }

        public static PlanViewModel GetCreatedPlan()
        {
            PlanViewModel planViewModel = new PlanViewModel();
            planViewModel.DaysPerWeek = Plan.DaysPerWeek;
            planViewModel.HoursPerDay = Plan.HoursPerDay;

            for (byte day = 0; day < Plan.DaysPerWeek; day++)
            {
                for (byte hour = 0; hour < Plan.HoursPerDay; hour++)
                {
                    foreach (var p in createdPlan.HourPlans[day, hour].ClassToTeacher)
                    {
                        LessonsViewModel lessonsViewModel = new LessonsViewModel();
                        lessonsViewModel.Day = day;
                        lessonsViewModel.Hour = hour;
                        lessonsViewModel.Subject = p.Value.subject;
                        lessonsViewModel.Teacher = p.Value.teacher;
                        string className = p.Key.Grade + p.Key.ClassName;
                        lessonsViewModel.Class = className;

                        planViewModel.LessonsViewModels.Add(lessonsViewModel);
                    }
                }
            }

            return planViewModel;
        }
    }

    /// <summary>
    /// Фитнесс функции
    /// </summary>
    public static class FitnessFunctions
    {
        public static int GroupWindowPenalty = 10;//штраф за окно у группы
        public static int TeacherWindowPenalty = 7;//штраф за окно у преподавателя
        public static int LateLessonPenalty = 1;//штраф за позднюю пару

        public static int LatesetHour = 4;//максимальный час, когда удобно проводить пары

        /// <summary>
        /// Штраф за окна
        /// </summary>
        public static int Windows(Plan plan)
        {
            var res = 0;

            for (byte day = 0; day < Plan.DaysPerWeek; day++)
            {
                List<TheClass> classHasLessons = new List<TheClass>();
                List<TheAppointment> appointmentHasLesson = new List<TheAppointment>();

                byte hour = 0;

                foreach (var lesson in plan.HourPlans[day, hour].ClassToTeacher)
                {
                    var theClass = lesson.Key;
                    var theAppointment = lesson.Value;

                    if (!classHasLessons.Contains(theClass))
                        classHasLessons.Add(theClass);

                    if (!appointmentHasLesson.Contains(theAppointment))
                        appointmentHasLesson.Add(theAppointment);
                }

                for (hour = 1; hour < Plan.HoursPerDay; hour++)
                {
                    foreach (var lesson in plan.HourPlans[day, hour].ClassToTeacher)
                    {
                        var theClass = lesson.Key;
                        var theAppointment = lesson.Value;

                        if (!classHasLessons.Contains(theClass))
                            classHasLessons.Add(theClass);
                        else if (!plan.HourPlans[day, hour - 1].ClassToTeacher.ContainsKey(theClass))
                            res += GroupWindowPenalty;

                        if (!appointmentHasLesson.Contains(theAppointment))
                            appointmentHasLesson.Add(theAppointment);
                        else if (!plan.HourPlans[day, hour - 1].TeacherToClass.ContainsKey(theAppointment))
                            res += TeacherWindowPenalty;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Штраф за поздние пары
        /// </summary>
        public static int LateLesson(Plan plan)
        {
            var res = 0;
            foreach (var pair in plan.GetLessons())
                if (pair.Hour > LatesetHour)
                    res += LateLessonPenalty;

            return res;
        }
    }

    /// <summary>
    /// Решатель (генетический алгоритм)
    /// </summary>
    public class Solver
    {
        public int MaxIterations = 1000;
        public int PopulationCount = 100;//должно делиться на 4

        public List<Func<Plan, int>> FitnessFunctions = new List<Func<Plan, int>>();

        public int Fitness(Plan plan)
        {
            var res = 0;

            foreach (var f in FitnessFunctions)
                res += f(plan);

            return res;
        }

        public Plan Solve(List<Plan> plans)
        {
            //создаем популяцию
            var pop = new Population();
            foreach (Plan plan in plans)
            {
                pop.AddPlan(plan);
            }

            //
            var count = MaxIterations;
            while (count-- > 0)
            {
                //считаем фитнесс функцию для всех планов
                pop.ForEach(p => p.FitnessValue = Fitness(p));
                //сортруем популяцию по фитнесс функции
                pop.Sort((p1, p2) => p1.FitnessValue.CompareTo(p2.FitnessValue));
                //найден идеальный план?
                if (pop[0].FitnessValue == 0)
                    return pop[0];
                //отбираем 25% лучших планов
                pop.RemoveRange(pop.Count / 4, pop.Count - pop.Count / 4);
                //от каждого создаем трех потомков с мутациями
                var c = pop.Count;
                for (int i = 0; i < c; i++)
                {
                    pop.AddChildOfParent(pop[i]);
                    pop.AddChildOfParent(pop[i]);
                    pop.AddChildOfParent(pop[i]);
                }

                if (count % 100 == 0)
                {
                    Console.WriteLine("Iterations left: {0}", count);
                }
            }

            //считаем фитнесс функцию для всех планов
            pop.ForEach(p => p.FitnessValue = Fitness(p));
            //сортруем популяцию по фитнесс функции
            pop.Sort((p1, p2) => p1.FitnessValue.CompareTo(p2.FitnessValue));

            //возвращаем лучший план
            return pop[0];
        }
    }

    /// <summary>
    /// Популяция планов
    /// </summary>
    public class Population : List<Plan>
    {
        public void AddPlan(Plan plan)
        {
            Add(plan);
        }

        public bool AddChildOfParent(Plan parent)
        {
            int maxIterations = 10;

            do
            {
                var plan = new Plan();
                if (plan.Init(parent))
                {
                    Add(plan);
                    return true;
                }
            } while (maxIterations-- > 0);
            return false;
        }
    }

    /// <summary>
    /// План занятий
    /// </summary>
    public class Plan
    {
        public Plan()
        {
            for (int i = 0; i < HoursPerDay; i++)
                for (int j = 0; j < DaysPerWeek; j++)
                    HourPlans[j, i] = new HourPlan();
        }

        public static int DaysPerWeek = 6;//6 учебных дня в неделю
        public static int HoursPerDay = 6;//до 6 уроков в день

        static Random rnd = new Random(3);

        /// <summary>
        ///  Задание учебных дней
        /// </summary>
        public static void SetStudyingSystem(int daysPerWeek, int hoursPerDay)
        {
            DaysPerWeek = daysPerWeek;
            HoursPerDay = hoursPerDay;
        }

        /// <summary>
        /// План по дням (первый индекс) и часам (второй индекс)
        /// </summary>
        public HourPlan[,] HourPlans = new HourPlan[DaysPerWeek, HoursPerDay];

        public int FitnessValue { get; internal set; }

        public bool AddLesson(Lessоn les)
        {
            return HourPlans[les.Day, les.Hour].AddLesson(les.TheClass, les.Appointment);
        }

        public void RemoveLesson(Lessоn les)
        {
            HourPlans[les.Day, les.Hour].RemoveLesson(les.TheClass, les.Appointment);
        }

        /// <summary>
        /// Добавить группу с преподом на любой час
        /// </summary>
        bool AddToAnyHour(byte day, TheClass theClass, TheAppointment appointment)
        {
            for (byte hour = 0; hour < HoursPerDay; hour++)
            {
                var les = new Lessоn(day, hour, theClass, appointment);
                if (AddLesson(les))
                    return true;
            }

            return false;//нет свободных часов в этот день
        }

        /// <summary>
        /// Создание наследника с мутацией
        /// </summary>
        public bool Init(Plan parent)
        {
            //копируем предка
            for (int i = 0; i < HoursPerDay; i++)
                for (int j = 0; j < DaysPerWeek; j++)
                    HourPlans[j, i] = parent.HourPlans[j, i].Clone();

            //выбираем два случайных дня
            var day1 = (byte)rnd.Next(DaysPerWeek);
            var day2 = (byte)rnd.Next(DaysPerWeek);

            //находим пары в эти дни
            var pairs1 = GetLessonsOfDay(day1).ToList();
            var pairs2 = GetLessonsOfDay(day2).ToList();

            //выбираем случайные пары
            if (pairs1.Count == 0 || pairs2.Count == 0) return false;
            var pair1 = pairs1[rnd.Next(pairs1.Count)];
            var pair2 = pairs2[rnd.Next(pairs2.Count)];

            //создаем мутацию - переставляем случайные пары местами
            RemoveLesson(pair1);//удаляем
            RemoveLesson(pair2);//удаляем
            var res1 = AddToAnyHour(pair2.Day, pair1.TheClass, pair1.Appointment);//вставляем в случайное место
            var res2 = AddToAnyHour(pair1.Day, pair2.TheClass, pair2.Appointment);//вставляем в случайное место
            return res1 && res2;
        }

        public IEnumerable<Lessоn> GetLessonsOfDay(byte day)
        {
            for (byte hour = 0; hour < HoursPerDay; hour++)
                foreach (var p in HourPlans[day, hour].ClassToTeacher)
                    yield return new Lessоn(day, hour, p.Key, p.Value);
        }

        public IEnumerable<Lessоn> GetLessons()
        {
            for (byte day = 0; day < DaysPerWeek; day++)
                for (byte hour = 0; hour < HoursPerDay; hour++)
                    foreach (var p in HourPlans[day, hour].ClassToTeacher)
                        yield return new Lessоn(day, hour, p.Key, p.Value);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (byte day = 0; day < Plan.DaysPerWeek; day++)
            {
                sb.AppendFormat("Day {0}\r\n", day);
                for (byte hour = 0; hour < Plan.HoursPerDay; hour++)
                {
                    sb.AppendFormat("Hour {0}: ", hour);
                    foreach (var p in HourPlans[day, hour].ClassToTeacher)
                        sb.AppendFormat("Class-Teacher-Subject: {0}{1}-{2}-{3} ", p.Key.Grade, p.Key.ClassName, p.Value.teacher, p.Value.subject);
                    sb.AppendLine();
                }
            }

            sb.AppendFormat("Fitness: {0}\r\n", FitnessValue);

            return sb.ToString();
        }
    }

    /// <summary>
    /// План на час
    /// </summary>
    public class HourPlan
    {
        /// <summary>
        /// Хранит пару группа-преподаватель
        /// </summary>
        public Dictionary<TheClass, TheAppointment> ClassToTeacher = new Dictionary<TheClass, TheAppointment>();

        /// <summary>
        /// Хранит пару преподаватель-группа
        /// </summary>
        public Dictionary<TheAppointment, TheClass> TeacherToClass = new Dictionary<TheAppointment, TheClass>();

        public bool AddLesson(TheClass theClass, TheAppointment teacher)
        {
            if (TeacherToClass.ContainsKey(teacher) || ClassToTeacher.ContainsKey(theClass))
                return false;//в этот час уже есть пара у препода или у группы

            ClassToTeacher[theClass] = teacher;
            TeacherToClass[teacher] = theClass;

            return true;
        }

        public void RemoveLesson(TheClass theClass, TheAppointment teacher)
        {
            ClassToTeacher.Remove(theClass);
            TeacherToClass.Remove(teacher);
        }

        public HourPlan Clone()
        {
            var res = new HourPlan();
            res.ClassToTeacher = new Dictionary<TheClass, TheAppointment>(ClassToTeacher);
            res.TeacherToClass = new Dictionary<TheAppointment, TheClass>(TeacherToClass);

            return res;
        }
    }

    /// <summary>
    /// Пара
    /// </summary>
    public class Lessоn
    {
        public byte Day = 255;
        public byte Hour = 255;
        public TheClass TheClass;
        public TheAppointment Appointment;

        public Lessоn(byte day, byte hour, TheClass theClass, TheAppointment appointment)
            : this(theClass, appointment)
        {
            Day = day;
            Hour = hour;
        }

        public Lessоn(TheClass theClass, TheAppointment appointment)
        {
            TheClass = theClass;
            Appointment = appointment;
        }
    }

    /// <summary>
    /// Класс
    /// </summary>
    public class TheClass : IEquatable<TheClass>
    {
        public int Grade;
        public string ClassName;

        public override bool Equals(object obj)
        {
            return Equals(obj as TheClass);
        }

        public bool Equals(TheClass other)
        {
            return other != null &&
                   Grade == other.Grade &&
                   ClassName == other.ClassName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Grade, ClassName);
        }
    }

    public class TheAppointment : IEquatable<TheAppointment>
    {
        public string teacher;
        public string subject;

        public override bool Equals(object obj)
        {
            return Equals(obj as TheAppointment);
        }

        public bool Equals(TheAppointment other)
        {
            return other != null &&
                   teacher == other.teacher &&
                   subject == other.subject;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(teacher, subject);
        }
    }
}
