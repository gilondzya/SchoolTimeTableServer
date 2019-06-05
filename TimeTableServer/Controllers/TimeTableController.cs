using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TimeTableServer.Models;
using System.IO;
using Newtonsoft.Json;

namespace TimeTableServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeTableController : ControllerBase
    {
        private PlanViewModel plan;

        // GET api/timetable
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PlanViewModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<IEnumerable<string>> GetSchedule()
        {
            if (plan != null)
                return Ok(plan);

            return BadRequest("schedule is null");
        }

        // POST: api/timetable
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public ActionResult<PlanViewModel> PostSchedule([FromBody]Input inputToAdd)
        {
            Scheduler scheduler = new Scheduler(inputToAdd);
            scheduler.CreateSchedule();
            GeneticScheduler.GeneticSchedulerMain(scheduler.plans);

            PlanViewModel planViewModel = GeneticScheduler.GetCreatedPlan();

            return CreatedAtAction("GetSchedule", planViewModel);
        }
    }
}