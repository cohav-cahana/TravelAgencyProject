using TravelAgencyProject.Models;
namespace TravelAgencyProject.Data
{
    public class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();
            if (context.Users.Any())
            {
                return;   // DB has been seeded
            }

            // Create Admin User
            var users = new User[]
            {
                new User{
                    FirstName="Admin",
                    LastName="Master",
                    Email="admin@gmail.com",
                    Password="tjrhfkvabho", // The  password for the admin אחרי כל השנים
                    IsAdmin=true 
                },
                new User{
                    FirstName="Maria",
                    LastName="badarne",
                    Email="user@gmail.com",
                    Password="1234",
                    IsAdmin=false // just a regular user
                }
            };
            foreach (User u in users)
            {
                context.Users.Add(u);
            }
            context.SaveChanges(); // save the users to the database
        }
    }
}
        
