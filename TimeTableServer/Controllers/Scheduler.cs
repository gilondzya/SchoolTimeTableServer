using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeTableServer.Models;

namespace TimeTableServer.Controllers
{
    public class Scheduler
    {
        public List<Plan> plans;

        public List<SchedulerAppointment> appointments;
        public int appointmentsNumber;

        private int lessonsPerDay = 6;
        private int studyingSystem = 6;

        public Scheduler(Input input)
        {
            appointmentsNumber = 0;
            InitializeSchedulerData(input);
        }

        // Method that transform JSON data from client to Scheduler data
        private void InitializeSchedulerData(Input input)
        {
            appointmentsNumber = 0;
            appointments = new List<SchedulerAppointment>();

            studyingSystem = input.StudyingSystem;
            lessonsPerDay = studyingSystem == 6 ? 6 : 7;

            foreach (Record record in input.Records)
            {
                int grade = (record.ClassName[1] >= '0' && record.ClassName[1] <= '1') ? Int32.Parse(record.ClassName.Substring(0, 2)) : Int32.Parse(record.ClassName.Substring(0, 1));
                string className = grade > 9 ? record.ClassName.Substring(2) : record.ClassName.Substring(1);

                foreach (Appointment appointment in record.Appointments)
                {
                    string subject = appointment.Subject;
                    string teacher = appointment.Teacher;

                    SchedulerAppointment schedulerAppointment = new SchedulerAppointment
                    {
                        Grade = grade,
                        ClassName = className,
                        Subject = subject,
                        Teacher = teacher
                    };

                    for (int i = 0; i < appointment.NumOfHours; i++)
                    {
                        appointmentsNumber++;
                        appointments.Add(schedulerAppointment);
                    }
                }
            }
        }

        public bool CreateSchedule()
        {
            if (appointmentsNumber == 0)
                return false;

            plans = new List<Plan>();

            if (studyingSystem == 6)
            {
                Plan.SetStudyingSystem(6, 6);

                // Initializing data storages
                int numberOfLessons = studyingSystem * lessonsPerDay;

                for (int planI = 0; planI < 100; planI++)
                {
                    int[,] graph = new int[appointmentsNumber, appointmentsNumber];
                    int[,] allowedColors = new int[appointmentsNumber, numberOfLessons];
                    List<int> verticesColors = new List<int>();

                    for (int i = 0; i < appointmentsNumber; i++)
                        for (int j = 0; j < appointmentsNumber; j++)
                            graph[i, j] = 0;

                    for (int i = 0; i < appointmentsNumber; i++)
                        for (int j = 0; j < numberOfLessons; j++)
                            allowedColors[i, j] = 0;

                    for (int i = 0; i < appointmentsNumber; i++)
                    {
                        verticesColors.Add(-1);
                    }

                    for (int i = 0; i < appointmentsNumber; i++)
                    {
                        for (int j = 0; j < appointmentsNumber; j++)
                        {
                            if (j == i)
                                graph[i, j] = 0;
                            else if (appointments[j].Grade == appointments[i].Grade && appointments[j].ClassName == appointments[i].ClassName)
                                graph[i, j] = 1;
                            else if (appointments[j].Teacher == appointments[i].Teacher)
                                graph[i, j] = 1;
                            else
                                graph[i, j] = 0;
                        }
                    }

                    for (int i = 0; i < appointmentsNumber; i++)
                    {
                        for (int j = 0; j < numberOfLessons; j++)
                        {
                            if (appointments[i].Grade == 1)
                            {
                                if (j > 23 || j % 6 == 5)
                                {
                                    allowedColors[i, j] = 1;
                                }
                            }
                            else if (appointments[i].Grade == 2)
                                if (j > 30 || j % 6 == 5)
                                {
                                    allowedColors[i, j] = 1;
                                }
                        }
                    }

                    for (int i = 0; i < appointmentsNumber; i++)
                    {
                        if (verticesColors[i] == -1)
                        {
                            for (int j = 0; j < numberOfLessons; j++)
                            {
                                if (allowedColors[i, j] == 0)
                                {
                                    verticesColors[i] = j;

                                    for (int k = 0; k < appointmentsNumber; k++)
                                    {
                                        if (graph[k, i] == 1)
                                            allowedColors[k, j] = 1;
                                    }
                                    break;
                                }
                            }

                            if (verticesColors[i] == -1)
                                verticesColors[i] = 0;
                        }
                    }

                    int penalty = 1;

                    while (penalty >= 10)
                    {
                        penalty = 0;

                        List<int> conflictVertices = new List<int>();

                        for (int i = 0; i < appointmentsNumber; i++)
                        {
                            for (int j = 0; j < appointmentsNumber; j++)
                            {
                                if (graph[i, j] == 1 && i != j && verticesColors[i] == verticesColors[j])
                                {
                                    if (!conflictVertices.Contains(i))
                                        conflictVertices.Add(i);
                                    if (!conflictVertices.Contains(j))
                                        conflictVertices.Add(j);
                                }
                            }
                        }

                        penalty = conflictVertices.Count;

                        foreach (int vertice in conflictVertices)
                        {
                            List<int> allowedColorsForVertice = new List<int>();
                            for (int i = 0; i < numberOfLessons; i++)
                            {
                                if (allowedColors[vertice, i] == 0)
                                    allowedColorsForVertice.Add(i);
                            }

                            Random random = new Random();

                            if (allowedColorsForVertice.Count > 0)
                            {
                                verticesColors[vertice] = allowedColorsForVertice[random.Next(allowedColorsForVertice.Count)];
                            }
                            else
                            {
                                if (appointments[vertice].Grade == 1)
                                {
                                    do
                                    {
                                        verticesColors[vertice] = random.Next(numberOfLessons);
                                    } while (verticesColors[vertice] > 23 || verticesColors[vertice] % 6 == 5);
                                }
                                else if (appointments[vertice].Grade == 2)
                                {
                                    do
                                    {
                                        verticesColors[vertice] = random.Next(numberOfLessons);
                                    } while (verticesColors[vertice] > 30 || verticesColors[vertice] % 6 == 5);
                                }
                                else
                                {
                                    verticesColors[vertice] = random.Next(numberOfLessons);
                                }
                            }
                        }
                    }

                    Console.WriteLine("Plan #{0} generated", planI);

                    var plan = new Plan();
                    for (int i = 0; i < appointmentsNumber; i++)
                    {
                        var theClass = new TheClass
                        {
                            Grade = appointments[i].Grade,
                            ClassName = appointments[i].ClassName
                        };
                        var theAppointment = new TheAppointment
                        {
                            subject = appointments[i].Subject,
                            teacher = appointments[i].Teacher
                        };

                        byte day = (byte)(verticesColors[i] % 6);
                        byte hour;

                        if (verticesColors[i] <= 5)
                            hour = 0;
                        else if (verticesColors[i] <= 11)
                            hour = 1;
                        else if (verticesColors[i] <= 17)
                            hour = 2;
                        else if (verticesColors[i] <= 23)
                            hour = 3;
                        else if (verticesColors[i] <= 29)
                            hour = 4;
                        else
                            hour = 5;

                        var lesson = new Lessоn(day, hour, theClass, theAppointment);
                        plan.AddLesson(lesson);
                    }

                    plans.Add(plan);
                }

                //GeneticScheduler.GeneticSchedulerMain(plans);
            }



            return true;
        }

        public class SchedulerAppointment
        {
            public int Grade { get; set; }
            public string ClassName { get; set; }
            public string Subject { get; set; }
            public string Teacher { get; set; }
        }

        // TO TEST --------------------------------------------------------
        private void PrintGraph(int[,] graph)
        {
            for (int i = 0; i < appointmentsNumber; i++)
            {
                Console.WriteLine("#{0}: {1}{2} - {3} - {4}", i, appointments[i].Grade, appointments[i].ClassName, appointments[i].Subject, appointments[i].Teacher);
            }

            for (int i = 0; i < appointmentsNumber; i++)
            {
                for (int j = 0; j < appointmentsNumber; j++)
                {
                    Console.Write(graph[i, j] + "  ");
                }
                Console.WriteLine();
            }
        }

        private void PrintAllowedColors(int[,] allowedColors)
        {
            for (int i = 0; i < appointmentsNumber; i++)
            {
                for (int j = 0; j < studyingSystem * lessonsPerDay; j++)
                {
                    Console.Write(allowedColors[i, j] + "  ");
                }
                Console.WriteLine();
            }
        }

        private void PrintVerticesColors(List<int> verticesColors)
        {
            for (int i = 0; i < appointmentsNumber; i++)
            {
                Console.Write(verticesColors[i] + "  ");
            }
        }
        // END TO TEST --------------------------------------------------------
    }
}
