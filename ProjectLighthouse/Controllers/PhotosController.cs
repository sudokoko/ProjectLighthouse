#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LBPUnion.ProjectLighthouse.Serialization;
using LBPUnion.ProjectLighthouse.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LBPUnion.ProjectLighthouse.Controllers
{
    [ApiController]
    [Route("LITTLEBIGPLANETPS3_XML/")]
    [Produces("text/xml")]
    public class PhotosController : ControllerBase
    {
        private readonly Database database;

        public PhotosController(Database database)
        {
            this.database = database;
        }

        [HttpPost("uploadPhoto")]
        public async Task<IActionResult> UploadPhoto()
        {
            User? user = await this.database.UserFromRequest(this.Request);
            if (user == null) return this.StatusCode(403, "");

            this.Request.Body.Position = 0;
            string bodyString = await new StreamReader(this.Request.Body).ReadToEndAsync();

            XmlSerializer serializer = new(typeof(Photo));
            Photo? photo = (Photo?)serializer.Deserialize(new StringReader(bodyString));
            if (photo == null) return this.BadRequest();

            photo.CreatorId = user.UserId;
            photo.Creator = user;

            foreach (PhotoSubject subject in photo.Subjects)
            {
                subject.User = await this.database.Users.FirstOrDefaultAsync(u => u.Username == subject.Username);

                if (subject.User == null) return this.BadRequest();

                subject.UserId = subject.User.UserId;

                this.database.PhotoSubjects.Add(subject);
            }

            await this.database.SaveChangesAsync();

            photo.PhotoSubjectCollection = photo.Subjects.Aggregate(string.Empty, (s, subject) => s + subject.PhotoSubjectId);
//            photo.Slot = await this.database.Slots.FirstOrDefaultAsync(s => s.SlotId == photo.SlotId);

            this.database.Photos.Add(photo);

            await this.database.SaveChangesAsync();

            return this.Ok();
        }

        [HttpGet("photos/user/{id:int}")]
        public async Task<IActionResult> SlotPhotos(int id)
        {
            List<Photo> photos = await this.database.Photos.Take(10).ToListAsync();
            string response = photos.Aggregate(string.Empty, (s, photo) => s + photo.Serialize(id));
            return this.Ok(LbpSerializer.StringElement("photos", response));
        }

        [HttpGet("photos/by")]
        public async Task<IActionResult> UserPhotos([FromQuery] string user, [FromQuery] int pageStart, [FromQuery] int pageSize)
        {
            User? userFromQuery = await this.database.Users.FirstOrDefaultAsync(u => u.Username == user);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (user == null) return this.NotFound();

            List<Photo> photos = await this.database.Photos.Where
                    (p => p.CreatorId == userFromQuery.UserId)
                .OrderByDescending(s => s.Timestamp)
                .Skip(pageStart - 1)
                .Take(Math.Min(pageSize, 30))
                .ToListAsync();
            string response = photos.Aggregate(string.Empty, (s, photo) => s + photo.Serialize(0));
            return this.Ok(LbpSerializer.StringElement("photos", response));
        }
    }
}