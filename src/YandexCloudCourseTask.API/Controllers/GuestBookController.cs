using Microsoft.AspNetCore.Mvc;
using YandexCloudCourseTask.API.Models;
using YandexCloudCourseTask.API.Models.Requests;
using YandexCloudCourseTask.API.Repositories;

namespace YandexCloudCourseTask.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class GuestbookController(YdbRepository repository, ReplicaInfo replicaInfo) : ControllerBase
    {
        [HttpGet("initial-state")]
        public ActionResult<ReplicaInfo> GetInitialState() =>Ok(replicaInfo);

        [HttpGet("messages")]
        public async Task<IEnumerable<Message>> GetMessages() => await repository.GetMessages();

        [HttpPost("messages")]
        public async Task<ActionResult> AddMessage([FromBody] AddMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content or username cannot be empty.");
            }

            await repository.AddMessage(request.Username, request.Content);
            return Created();
        }
    }
}