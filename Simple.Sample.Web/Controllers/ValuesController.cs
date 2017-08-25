using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WCommon.Messages;
using simple.esb;

namespace Simple.Sample.Web.Controllers
{
    [Route("api")]
    public class ValuesController : Controller
    {
        private readonly IServiceBus _bus;

        public ValuesController(IServiceBus bus)
        {
            _bus = bus;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "publish/123", "import/123", "train/123", "theworks/3" };
        }

        [HttpGet("publish/{id}")]
        public string Publish(int id)
        {
            Guid guid = MashToGuid(id);

            _bus.Send(new PublishAgent()
            {
                Id = guid
            });

            return guid.ToString("N"); ;
        }

        [HttpGet("import/{id}")]
        public string Import(int id)
        {
            Guid guid = MashToGuid(id);

            _bus.Send(new ImportEvaluatorData()
            {
                ImportId = guid
            });

            return guid.ToString("N");
        }

        [HttpGet("train/{id}")]
        public string Train(int id)
        {
            Guid guid = MashToGuid(id);

            _bus.Send(new TrainModels()
            {
                Id = guid
            });

            return guid.ToString("N");
        }

        [HttpGet("theworks/{count}")]
        public string TheWorks(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _bus.Send(new PublishAgent() { Id = MashToGuid(i) });
                _bus.Send(new ImportEvaluatorData() { ImportId = MashToGuid(i) });
                _bus.Send(new TrainModels() { Id = MashToGuid(i) });
            }

            return string.Format("{0} of each job was triggered", count);
        }


        private Guid MashToGuid(int number)
        {
            Guid guid = Guid.NewGuid();
            string guidString = guid.ToString("N");
            string numberString = number.ToString("D8");
            var mashup = numberString + guidString.Substring(numberString.Length);
            return new Guid(mashup);
        }
    }
}
