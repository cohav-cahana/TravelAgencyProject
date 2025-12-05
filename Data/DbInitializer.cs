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

            var trips = new Trip[]
            {
                new Trip{
                    Destination="Paris",
                    Country="France",
                    Description="A romantic trip to the city of lights, including a visit to the Eiffel Tower.",
                    StartDate=DateTime.Now.AddDays(10),
                    EndDate=DateTime.Now.AddDays(17),
                    Price=1500,
                    SalePrice=1200, 
                    Stock=5,
                    Category="Honeymoon",
                    ImageUrl="https://dummyimage.com/600x400/000/fff&text=Paris" // תמונה זמנית
                },
                new Trip{
                    Destination="London",
                    Country="UK",
                    Description="Explore the history of London, Big Ben and the London Eye.",
                    StartDate=DateTime.Now.AddDays(30),
                    EndDate=DateTime.Now.AddDays(37),
                    Price=2000,
                    SalePrice=null, 
                    Stock=10,
                    Category="Family",
                    ImageUrl="https://dummyimage.com/600x400/000/fff&text=London"
                }
            };

            foreach (Trip t in trips)
            {
                context.Trips.Add(t);
            }
            context.SaveChanges();
        }
    }
}
        
