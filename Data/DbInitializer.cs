using System;
using System.Linq;
using TravelAgencyProject.Models;
using Microsoft.EntityFrameworkCore;

namespace TravelAgencyProject.Data
{
    public class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // --- 1. SEED USERS ---
            if (!context.Users.Any())
            {
                var users = new User[]
                {
                    new User { FirstName="Admin", LastName="Master", Email="admin@gmail.com", Password="tjrhfkvabho", IsAdmin=true },
                    new User { FirstName="Maria", LastName="badarne", Email="user@gmail.com", Password="1234", IsAdmin=false }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // --- 2. SEED 25 TRIPS (With Upsert Logic) ---
            var trips = new Trip[]
            {
                // Europe
                new Trip { Destination = "Paris", Country = "France", Description = "Experience the romantic city of lights, visit the Eiffel Tower and enjoy fine dining.", StartDate = DateTime.Today.AddDays(30), EndDate = DateTime.Today.AddDays(37), Price = 1200, Stock = 1, Category = "Honeymoon", AgeLimitaion = 18, IsVisible = true, ImageUrl = "/images/trips/paris.jpg" },
                new Trip { Destination = "London", Country = "UK", Description = "Visit the Big Ben, London Eye and Buckingham Palace in a family friendly tour.", StartDate = DateTime.Today.AddDays(40), EndDate = DateTime.Today.AddDays(45), Price = 1500, Stock = 2, Category = "Family", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/039fe859-af96-44c5-b1a0-1b6d19074520_london.jpeg" },
                new Trip { Destination = "Rome", Country = "Italy", Description = "Discover the ancient history of the Colosseum and enjoy the best pizza in the world.", StartDate = DateTime.Today.AddDays(20), EndDate = DateTime.Today.AddDays(25), Price = 1100, Stock = 4, Category = "History", AgeLimitaion = 10, IsVisible = true, ImageUrl = "/images/trips/rome.jpg" },
                new Trip { Destination = "Barcelona", Country = "Spain", Description = "Enjoy the amazing architecture of Gaudi and the sunny beaches of Barcelona.", StartDate = DateTime.Today.AddDays(60), EndDate = DateTime.Today.AddDays(67), Price = 1300, Stock = 1, Category = "Vacation", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/d7593a87-86bb-4879-85d2-d547b5146a2e_BARZ.jpg" },
                new Trip { Destination = "Prague", Country = "Czech Republic", Description = "Walk through the Charles Bridge and explore the magical old town square.", StartDate = DateTime.Today.AddDays(15), EndDate = DateTime.Today.AddDays(20), Price = 900, SalePrice = 800, DiscountEndDate = DateTime.Today.AddDays(5), Stock = 5, Category = "City Break", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/d983a2cb-a27e-40c1-9a23-c521578b134b_Prague.jpg" },
                
                // Asia
                new Trip { Destination = "Tokyo", Country = "Japan", Description = "A perfect mix of modern technology and traditional culture in the heart of Japan.", StartDate = DateTime.Today.AddMonths(2), EndDate = DateTime.Today.AddMonths(2).AddDays(10), Price = 2500, Stock = 6, Category = "Adventure", AgeLimitaion = 12, IsVisible = true, ImageUrl = "/images/trips/1a707a4a-c639-4be5-9db4-a66faf6df191_japan.jpg" },
                new Trip { Destination = "Kyoto", Country = "Japan", Description = "Visit the beautiful temples and bamboo forests of ancient Kyoto.", StartDate = DateTime.Today.AddMonths(3), EndDate = DateTime.Today.AddMonths(3).AddDays(7), Price = 2300, Stock = 4, Category = "Culture", AgeLimitaion = 12, IsVisible = true, ImageUrl = "/images/trips/kyoto.jpg" },
                new Trip { Destination = "Bangkok", Country = "Thailand", Description = "Experience the vibrant street life, markets and temples of Bangkok.", StartDate = DateTime.Today.AddDays(50), EndDate = DateTime.Today.AddDays(60), Price = 1000, Stock = 20, Category = "Backpacking", AgeLimitaion = 18, IsVisible = true, ImageUrl = "/images/trips/bangkok.jpg" },
                new Trip { Destination = "Phuket", Country = "Thailand", Description = "Relax on the most beautiful beaches and enjoy water sports activities.", StartDate = DateTime.Today.AddDays(55), EndDate = DateTime.Today.AddDays(65), Price = 1200, Stock = 15, Category = "Beaches", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/phuket.jpg" },
                new Trip { Destination = "Seoul", Country = "South Korea", Description = "Dynamic city combining skyscrapers, high-tech subways and pop culture.", StartDate = DateTime.Today.AddDays(80), EndDate = DateTime.Today.AddDays(90), Price = 1800, Stock = 8, Category = "Urban", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/4f36f5a8-87b1-4183-9692-d95248f3b41e_Seoul.jpg" },

                // USA & Americas
                new Trip { Destination = "New York", Country = "USA", Description = "The city that never sleeps. Times Square, Central Park and Broadway shows.", StartDate = DateTime.Today.AddDays(25), EndDate = DateTime.Today.AddDays(32), Price = 2000, Stock = 10, Category = "Luxury", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/nyc.jpg" },
                new Trip { Destination = "Orlando", Country = "USA", Description = "The ultimate family vacation with Disney World and Universal Studios.", StartDate = DateTime.Today.AddMonths(4), EndDate = DateTime.Today.AddMonths(4).AddDays(14), Price = 3000, Stock = 25, Category = "Family", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/b1c801f7-9a50-40d4-a845-22e01e02cbb9_ORLANDO.jpg" },
                new Trip { Destination = "Las Vegas", Country = "USA", Description = "Entertainment, casinos, and spectacular shows in the middle of the desert.", StartDate = DateTime.Today.AddDays(100), EndDate = DateTime.Today.AddDays(105), Price = 1600, Stock = 20, Category = "Nightlife", AgeLimitaion = 21, IsVisible = true, ImageUrl = "/images/trips/vegas.jpg" },
                new Trip { Destination = "Cancun", Country = "Mexico", Description = "Crystal clear caribbean waters, luxury resorts and maya ruins.", StartDate = DateTime.Today.AddDays(45), EndDate = DateTime.Today.AddDays(52), Price = 1400, SalePrice = 1200, DiscountEndDate = DateTime.Today.AddDays(3), Stock = 12, Category = "Resort", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/cancun.jpg" },
                new Trip { Destination = "Rio", Country = "Brazil", Description = "Carnival, Copacabana beach and the Christ the Redeemer statue.", StartDate = DateTime.Today.AddMonths(5), EndDate = DateTime.Today.AddMonths(5).AddDays(10), Price = 1900, Stock = 8, Category = "Adventure", AgeLimitaion = 18, IsVisible = true, ImageUrl = "/images/trips/rio.jpg" },

                // Africa & ME
                new Trip { Destination = "Tel Aviv", Country = "Israel", Description = "Sunny beaches, vibrant nightlife and amazing culinary scene.", StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(17), Price = 800, Stock = 30, Category = "City Break", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/13ee5a60-8cc8-438d-be82-b66ad4f6e988_telaviv.jpeg" },
                new Trip { Destination = "Jerusalem", Country = "Israel", Description = "A spiritual journey through history in one of the oldest cities.", StartDate = DateTime.Today.AddDays(12), EndDate = DateTime.Today.AddDays(15), Price = 700, Stock = 20, Category = "History", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/jerusalem.jpg" },
                new Trip { Destination = "Dubai", Country = "UAE", Description = "Luxury shopping, ultramodern architecture and lively nightlife scene.", StartDate = DateTime.Today.AddDays(35), EndDate = DateTime.Today.AddDays(40), Price = 1500, Stock = 10, Category = "Luxury", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/dubai.jpg" },
                new Trip { Destination = "Nairobi", Country = "Kenya", Description = "Unforgettable Safari experience watching wild animals in nature.", StartDate = DateTime.Today.AddMonths(3), EndDate = DateTime.Today.AddMonths(3).AddDays(10), Price = 2800, Stock = 5, Category = "Safari", AgeLimitaion = 6, IsVisible = true, ImageUrl = "/images/trips/safari.jpg" },
                new Trip { Destination = "Cairo", Country = "Egypt", Description = "Visit the Great Pyramids of Giza and the Sphinx.", StartDate = DateTime.Today.AddDays(28), EndDate = DateTime.Today.AddDays(33), Price = 600, Stock = 15, Category = "History", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/cairo.jpg" },

                // Others
                new Trip { Destination = "Sydney", Country = "Australia", Description = "Explore the Opera House and the beautiful harbor of Sydney.", StartDate = DateTime.Today.AddMonths(6), EndDate = DateTime.Today.AddMonths(6).AddDays(14), Price = 3500, Stock = 5, Category = "Adventure", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/sydney.jpg" },
                new Trip { Destination = "Auckland", Country = "New Zealand", Description = "BBreathtaking landscapes and outdoor activities for nature lovers.", StartDate = DateTime.Today.AddMonths(6).AddDays(5), EndDate = DateTime.Today.AddMonths(6).AddDays(20), Price = 3600, Stock = 4, Category = "Nature", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/nz.jpg" },
                new Trip { Destination = "Maldives", Country = "Maldives", Description = "The ultimate relaxation in private bungalows over clear blue water.", StartDate = DateTime.Today.AddMonths(2), EndDate = DateTime.Today.AddMonths(2).AddDays(7), Price = 4000, Stock = 3, Category = "Honeymoon", AgeLimitaion = 18, IsVisible = true, ImageUrl = "/images/trips/maldives.jpg" },
                new Trip { Destination = "Santorini", Country = "Greece", Description = "White buildings, blue domes and amazing sunsets over the Aegean Sea.", StartDate = DateTime.Today.AddDays(50), EndDate = DateTime.Today.AddDays(57), Price = 1300, Stock = 8, Category = "Romantic", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/santorini.jpg" },
                new Trip { Destination = "Paphos", Country = "Cyprus", Description = "A coastal city in the southwest of Cyprus and amazing beaches.", StartDate = DateTime.Today.AddDays(15), EndDate = DateTime.Today.AddDays(20), Price = 500, Stock = 20, Category = "Vacation", AgeLimitaion = 0, IsVisible = true, ImageUrl = "/images/trips/e981da9c-f7da-4551-8f3f-e4e57e28ac80_papos.jpg" }
            };

            foreach (var t in trips)
            {
                if (!context.Trips.Any(x => x.Destination == t.Destination && x.StartDate == t.StartDate))
                {
                    context.Trips.Add(t);
                }
            }
            context.SaveChanges();

            // --- 3. SEED REVIEWS ---
            if (!context.Reviews.Any())
            {
                var reviews = new Review[]
                {
                    new Review { TripId = 1, UserId = 2, Rating = 5, Comment = "A truly magical experience!", PostedDate = DateTime.Now.AddDays(-10) },
                    new Review { TripId = 2, UserId = 2, Rating = 4, Comment = "Great family tour of London!", PostedDate = DateTime.Now.AddDays(-7) },
                    new Review { TripId = 6, UserId = 2, Rating = 5, Comment = "Tokyo is mind-blowing!", PostedDate = DateTime.Now.AddDays(-3) },
                    new Review { TripId = 11, UserId = 2, Rating = 5, Comment = "Best trip to NY ever!", PostedDate = DateTime.Now.AddDays(-1) },
                    new Review { TripId = 24, UserId = 2, Rating = 5, Comment = "Beautiful Santorini sunset!", PostedDate = DateTime.Now.AddHours(-5) }
                };
                context.Reviews.AddRange(reviews);
                context.SaveChanges();
            }
        }
    }
}