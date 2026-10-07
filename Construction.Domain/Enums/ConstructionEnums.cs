namespace Construction.Domain.Enums;

public enum UserRole
{
    Client,
    Admin
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    OutForDelivery,
    Delivered,
    Cancelled
}

public enum PaymentStatus
{
    Unpaid,
    Paid
}

public enum ProjectStatus
{
    Planned,
    InProgress,
    Completed,
    OnHold
}

public enum EquipmentStatus
{
    Available,
    Rented,
    Maintenance
}

public enum RentalStatus
{
    Requested,
    Active,
    Returned,
    Cancelled
}
