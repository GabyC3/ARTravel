using ARTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ARTravel.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Passenger> Passengers => Set<Passenger>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<ReservationPassenger> ReservationPassengers => Set<ReservationPassenger>();

    public DbSet<ReservationCancellation> ReservationCancellations => Set<ReservationCancellation>();

    public DbSet<ReservationPayment> ReservationPayments => Set<ReservationPayment>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<Destination> Destinations => Set<Destination>();

    public DbSet<DestinationImage> DestinationImages => Set<DestinationImage>();

    public DbSet<TourismProduct> Products => Set<TourismProduct>();

    public DbSet<ProductDestination> ProductDestinations => Set<ProductDestination>();

    public DbSet<TourismProductImage> TourismProductImages => Set<TourismProductImage>();

    public DbSet<Departure> Departures => Set<Departure>();

    public DbSet<DeparturePrice> DeparturePrices => Set<DeparturePrice>();

    public DbSet<Itinerary> Itineraries => Set<Itinerary>();

    public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();

    public DbSet<ItineraryActivity> ItineraryActivities => Set<ItineraryActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.TokenVersion).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(30);
            entity.HasIndex(user => user.IsActive);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(50).IsRequired();
            entity.Property(role => role.NormalizedName).HasMaxLength(50).IsRequired();
            entity.HasIndex(role => role.NormalizedName).IsUnique();
            entity.Property(role => role.Description).HasMaxLength(250);
            entity.HasData(
                new Role
                {
                    Id = 1,
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Standard user",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = 2,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Administrator",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RevokedAccessToken>(entity =>
        {
            entity.HasKey(token => token.TokenId);
            entity.Property(token => token.TokenId).HasMaxLength(64);
            entity.HasIndex(token => token.ExpiresAt);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.TokenHash);
            entity.Property(token => token.TokenHash).HasMaxLength(64);
            entity.Property(token => token.SessionId).HasMaxLength(64).IsRequired();
            entity.HasIndex(token => token.UserId);
            entity.HasIndex(token => token.SessionId);
            entity.HasIndex(token => token.ExpiresAt);
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(token => token.TokenHash);
            entity.Property(token => token.TokenHash).HasMaxLength(64);
            entity.HasIndex(token => token.UserId);
            entity.HasIndex(token => token.ExpiresAt);
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.CustomerType).IsRequired();
            entity.Property(customer => customer.FirstName).HasMaxLength(100);
            entity.Property(customer => customer.LastName).HasMaxLength(100);
            entity.Property(customer => customer.BusinessName).HasMaxLength(200);
            entity.Property(customer => customer.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(customer => customer.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(customer => customer.Email).HasMaxLength(320);
            entity.Property(customer => customer.PhoneNumber).HasMaxLength(30);
            entity.Property(customer => customer.Address).HasMaxLength(250);
            entity.Property(customer => customer.City).HasMaxLength(100);
            entity.Property(customer => customer.Country).HasMaxLength(100);
            entity.Property(customer => customer.Notes).HasMaxLength(2000);
            entity.HasIndex(customer => customer.IsActive);
            entity.HasIndex(customer => new { customer.DocumentType, customer.DocumentNumber });
        });

        modelBuilder.Entity<Passenger>(entity =>
        {
            entity.HasKey(passenger => passenger.Id);
            entity.Property(passenger => passenger.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(passenger => passenger.LastName).HasMaxLength(100).IsRequired();
            entity.Property(passenger => passenger.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(passenger => passenger.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(passenger => passenger.Nationality).HasMaxLength(100);
            entity.Property(passenger => passenger.Email).HasMaxLength(320);
            entity.Property(passenger => passenger.PhoneNumber).HasMaxLength(30);
            entity.Property(passenger => passenger.EmergencyContactName).HasMaxLength(200);
            entity.Property(passenger => passenger.EmergencyContactPhone).HasMaxLength(30);
            entity.Property(passenger => passenger.Notes).HasMaxLength(2000);
            entity.HasIndex(passenger => passenger.IsActive);
            entity.HasIndex(passenger => new { passenger.DocumentType, passenger.DocumentNumber });
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.ReservationNumber).HasMaxLength(40).IsRequired();
            entity.Property(reservation => reservation.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(reservation => reservation.Currency).HasMaxLength(3).IsRequired();
            entity.Property(reservation => reservation.Notes).HasMaxLength(2000);
            entity.Property(reservation => reservation.Subtotal).HasPrecision(12, 2);
            entity.Property(reservation => reservation.Discount).HasPrecision(12, 2);
            entity.Property(reservation => reservation.TotalAmount).HasPrecision(12, 2);
            entity.HasIndex(reservation => reservation.ReservationNumber).IsUnique();
            entity.HasIndex(reservation => new { reservation.CustomerId, reservation.ReservationDate });
            entity.HasIndex(reservation => new { reservation.DepartureId, reservation.Status });
            entity.HasIndex(reservation => reservation.CreatedByUserId);
            entity.HasAlternateKey(reservation => new { reservation.Id, reservation.DepartureId });
            entity.HasOne(reservation => reservation.Customer)
                .WithMany(customer => customer.Reservations)
                .HasForeignKey(reservation => reservation.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reservation => reservation.Departure)
                .WithMany(departure => departure.Reservations)
                .HasForeignKey(reservation => reservation.DepartureId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(reservation => reservation.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReservationPassenger>(entity =>
        {
            entity.HasKey(passenger => passenger.Id);
            entity.Property(passenger => passenger.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(passenger => passenger.LastName).HasMaxLength(100).IsRequired();
            entity.Property(passenger => passenger.DocumentType).IsRequired();
            entity.Property(passenger => passenger.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(passenger => passenger.Gender).HasMaxLength(50);
            entity.Property(passenger => passenger.PhoneNumber).HasMaxLength(30);
            entity.Property(passenger => passenger.Email).HasMaxLength(320);
            entity.Property(passenger => passenger.EmergencyContactName).HasMaxLength(200);
            entity.Property(passenger => passenger.EmergencyContactPhone).HasMaxLength(30);
            entity.Property(passenger => passenger.SpecialRequirements).HasMaxLength(2000);
            entity.Property(passenger => passenger.Notes).HasMaxLength(2000);
            entity.Property(passenger => passenger.PriceType).IsRequired();
            entity.Property(passenger => passenger.UnitPrice).HasPrecision(12, 2);
            entity.Property(passenger => passenger.Currency).HasMaxLength(3).IsRequired();
            entity.HasIndex(passenger => new
            {
                passenger.ReservationId,
                passenger.DocumentType,
                passenger.DocumentNumber
            })
                .HasDatabaseName("IX_ReservationPassengers_Reservation_Document")
                .IsUnique();
            entity.HasIndex(passenger => passenger.PassengerId);
            entity.HasAlternateKey(passenger => new { passenger.Id, passenger.ReservationId });
            entity.HasOne(passenger => passenger.Reservation)
                .WithMany(reservation => reservation.Passengers)
                .HasForeignKey(passenger => passenger.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(passenger => passenger.Passenger)
                .WithMany(passenger => passenger.ReservationPassengers)
                .HasForeignKey(passenger => passenger.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReservationCancellation>(entity =>
        {
            entity.HasKey(cancellation => cancellation.Id);
            entity.Property(cancellation => cancellation.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(cancellation => cancellation.RefundAmount).HasPrecision(12, 2);
            entity.HasIndex(cancellation => cancellation.ReservationId);
            entity.HasIndex(cancellation => cancellation.CancelledByUserId);
            entity.HasOne(cancellation => cancellation.Reservation)
                .WithMany(reservation => reservation.Cancellations)
                .HasForeignKey(cancellation => cancellation.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(cancellation => cancellation.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReservationPayment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
            entity.Property(payment => payment.PaymentMethod).HasMaxLength(50).IsRequired();
            entity.Property(payment => payment.Status).IsRequired();
            entity.Property(payment => payment.TransactionReference).HasMaxLength(200);
            entity.Property(payment => payment.Notes).HasMaxLength(2000);
            entity.Property(payment => payment.Amount).HasPrecision(12, 2);
            entity.HasIndex(payment => new { payment.ReservationId, payment.CreatedAt });
            entity.HasOne(payment => payment.Reservation)
                .WithMany()
                .HasForeignKey(payment => payment.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(trip => trip.Id);
            entity.Property(trip => trip.Origin).HasMaxLength(200).IsRequired();
            entity.Property(trip => trip.Destination).HasMaxLength(200).IsRequired();
            entity.Property(trip => trip.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(trip => trip.PassengerId);
            entity.HasIndex(trip => trip.ReservationId);
            entity.HasIndex(trip => trip.DepartureId);
            entity.HasOne(trip => trip.Reservation)
                .WithMany(reservation => reservation.Trips)
                .HasForeignKey(trip => trip.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(trip => trip.Passenger)
                .WithMany()
                .HasForeignKey(trip => trip.PassengerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(trip => trip.Departure)
                .WithMany(departure => departure.Trips)
                .HasForeignKey(trip => trip.DepartureId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(trip => trip.ReservationPassenger)
                .WithOne(passenger => passenger.Trip)
                .HasForeignKey<Trip>(trip => trip.ReservationPassengerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Reservation>()
                .WithMany()
                .HasForeignKey("ReservationId", "DepartureId")
                .HasPrincipalKey("Id", "DepartureId")
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ReservationPassenger>()
                .WithMany()
                .HasForeignKey("ReservationPassengerId", "ReservationId")
                .HasPrincipalKey("Id", "ReservationId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.HasKey(destination => destination.Id);
            entity.Property(destination => destination.Name).HasMaxLength(200).IsRequired();
            entity.Property(destination => destination.Description).HasMaxLength(2000);
            entity.Property(destination => destination.DestinationType).IsRequired();
            entity.Property(destination => destination.Country).HasMaxLength(100).IsRequired();
            entity.Property(destination => destination.Province).HasMaxLength(100);
            entity.Property(destination => destination.City).HasMaxLength(100);
            entity.Property(destination => destination.Address).HasMaxLength(300);
            entity.Property(destination => destination.Latitude).HasPrecision(9, 6);
            entity.Property(destination => destination.Longitude).HasPrecision(9, 6);
            entity.HasIndex(destination => destination.IsActive);
            entity.HasIndex(destination => destination.DestinationType);
            entity.HasIndex(destination => destination.ParentDestinationId);
            entity.HasOne<Destination>()
                .WithMany()
                .HasForeignKey(destination => destination.ParentDestinationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DestinationImage>(entity =>
        {
            entity.HasKey(image => image.Id);
            entity.Property(image => image.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(image => image.AltText).HasMaxLength(300);
            entity.HasIndex(image => new { image.DestinationId, image.DisplayOrder });
            entity.HasOne<Destination>()
                .WithMany()
                .HasForeignKey(image => image.DestinationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TourismProduct>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Code).HasMaxLength(50).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(4000);
            entity.Property(product => product.ProductType).IsRequired();
            entity.Property(product => product.DurationDays).IsRequired();
            entity.Property(product => product.DurationNights).IsRequired();
            entity.Property(product => product.BasePrice)
                .HasColumnName("Price")
                .HasPrecision(12, 2);
            entity.Property(product => product.Currency).HasMaxLength(3).IsRequired();
            entity.Property(product => product.MinimumPassengers).IsRequired();
            entity.Property(product => product.Includes).HasMaxLength(4000);
            entity.Property(product => product.Excludes).HasMaxLength(4000);
            entity.Property(product => product.Recommendations).HasMaxLength(4000);
            entity.Property(product => product.CancellationPolicy).HasMaxLength(4000);
            entity.HasIndex(product => product.Code).IsUnique();
            entity.HasIndex(product => product.IsActive);
            entity.HasIndex(product => product.IsPublished);
        });

        modelBuilder.Entity<ProductDestination>(entity =>
        {
            entity.HasKey(link => new { link.ProductId, link.DestinationId });
            entity.Property(link => link.Notes).HasMaxLength(1000);
            entity.HasIndex(link => new { link.ProductId, link.VisitOrder });
            entity.HasOne(link => link.TourismProduct)
                .WithMany()
                .HasForeignKey(link => link.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Destination)
                .WithMany()
                .HasForeignKey(link => link.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TourismProductImage>(entity =>
        {
            entity.HasKey(image => image.Id);
            entity.Property(image => image.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(image => image.AltText).HasMaxLength(300);
            entity.HasIndex(image => new { image.ProductId, image.DisplayOrder });
            entity.HasOne<TourismProduct>()
                .WithMany()
                .HasForeignKey(image => image.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Departure>(entity =>
        {
            entity.HasKey(departure => departure.Id);
            entity.Property(departure => departure.MeetingPoint).HasMaxLength(300).IsRequired();
            entity.Property(departure => departure.Status).IsRequired();
            entity.Property(departure => departure.Currency).HasMaxLength(3).IsRequired();
            entity.Property(departure => departure.Notes).HasMaxLength(2000);
            entity.Property(departure => departure.BasePrice).HasPrecision(12, 2);
            entity.HasIndex(departure => new { departure.ProductId, departure.DepartureDate });
            entity.HasOne(departure => departure.Product)
                .WithMany()
                .HasForeignKey(departure => departure.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(departure => departure.Itinerary)
                .WithMany()
                .HasForeignKey(departure => departure.ItineraryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeparturePrice>(entity =>
        {
            entity.HasKey(price => price.Id);
            entity.Property(price => price.PriceType).IsRequired();
            entity.Property(price => price.Amount).HasPrecision(12, 2);
            entity.Property(price => price.Currency).HasMaxLength(3).IsRequired();
            entity.Property(price => price.Description).HasMaxLength(500);
            entity.HasIndex(price => new { price.DepartureId, price.PriceType });
            entity.HasOne(price => price.Departure)
                .WithMany(departure => departure.Prices)
                .HasForeignKey(price => price.DepartureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Itinerary>(entity =>
        {
            entity.HasKey(itinerary => itinerary.Id);
            entity.Property(itinerary => itinerary.Name).HasMaxLength(200).IsRequired();
            entity.Property(itinerary => itinerary.Description).HasMaxLength(4000);
            entity.Property(itinerary => itinerary.Version)
                .IsRequired()
                .IsConcurrencyToken();
            entity.HasIndex(itinerary => new { itinerary.ProductId, itinerary.IsActive });
            entity.HasOne(itinerary => itinerary.Product)
                .WithMany(product => product.Itineraries)
                .HasForeignKey(itinerary => itinerary.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ItineraryDay>(entity =>
        {
            entity.HasKey(day => day.Id);
            entity.Property(day => day.Title).HasMaxLength(200).IsRequired();
            entity.Property(day => day.Description).HasMaxLength(4000);
            entity.HasIndex(day => new { day.ItineraryId, day.DayNumber });
            entity.HasOne(day => day.Itinerary)
                .WithMany(itinerary => itinerary.Days)
                .HasForeignKey(day => day.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItineraryActivity>(entity =>
        {
            entity.HasKey(activity => activity.Id);
            entity.Property(activity => activity.Name).HasMaxLength(200).IsRequired();
            entity.Property(activity => activity.Description).HasMaxLength(4000);
            entity.Property(activity => activity.ActivityType).IsRequired();
            entity.Property(activity => activity.Notes).HasMaxLength(2000);
            entity.HasIndex(activity => new { activity.ItineraryDayId, activity.DisplayOrder });
            entity.HasOne(activity => activity.ItineraryDay)
                .WithMany(day => day.Activities)
                .HasForeignKey(activity => activity.ItineraryDayId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Destination>()
                .WithMany()
                .HasForeignKey(activity => activity.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
