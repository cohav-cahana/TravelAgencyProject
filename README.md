# 🌍 Travel Agency Booking Platform

A full-stack ASP.NET Core MVC application for managing and booking international travel packages. This platform provides a seamless experience for users to browse trips, manage a shopping cart, and complete secure payments.

## 🚀 Key Features

* **Dynamic Trip Catalog:** Browse trips by destination, category, and price.
* **Shopping Cart:** Add multiple trips to a session-based cart before checkout.
* **Secure Checkout:** Fully integrated with **PayPal SDK** for real-time payments.
* **PDF Generation:** Automated generation of professional **Booking Receipts** and **Travel Itineraries** using `pdfMake`.
* **Email Notifications:** Automatic email confirmation sent via **Gmail SMTP** upon successful booking.
* **User Management:** Secure login and registration (via Identity/Session).
* **Admin Dashboard:** Full CRUD operations for managing trips and viewing bookings.
* **Waiting List:** Logic for users to join a waiting list when a trip is fully booked.

## 🛠️ Tech Stack

* **Backend:** C# | ASP.NET Core 8.0 MVC
* **Database:** Entity Framework Core | SQL Server (LocalDB)
* **Frontend:** Bootstrap 5 | JavaScript | HTML5 & CSS3
* **Libraries:** * `pdfMake` for high-quality PDF exports.
    * `Newtonsoft.Json` for data serialization.
    * `Microsoft.Extensions.Configuration` for secure settings.

## 🔐 Security & Configuration

To protect sensitive data, this project uses **User Secrets**. To run the project locally, you must configure the following keys:

```json
{
  "PayPal": {
    "ClientId": "YOUR_PAYPAL_CLIENT_ID",
    "Secret": "YOUR_PAYPAL_SECRET"
  },
  "EmailSettings": {
    "SmtpUser": "YOUR_EMAIL@gmail.com",
    "SmtpPass": "YOUR_GMAIL_APP_PASSWORD"
  }
}
```
 ## 📖 How to Run:

* Clone the repository to your local machine.

* Open the solution in Visual Studio 2022.

* Right-click the project and select "Manage User Secrets" to add your credentials (as shown in the Security section above).

* Run Update-Database in the Package Manager Console to set up the local SQL database.

* Press F5 to launch the application.

Developed by Cohav Kahana and Maria Badarne as part of the Web Development Course.
