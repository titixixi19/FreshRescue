using FreshRescue.DataAccess.Repositories;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore.Storage;

namespace FreshRescue.DataAccess.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly FreshRescueDbContext _context;

    // Tạo khi dùng tới lần đầu: một request thường chỉ chạm vài bảng, không cần dựng đủ 17.
    private IGenericRepository<AppUser>? _users;
    private IGenericRepository<RefreshToken>? _refreshTokens;
    private IGenericRepository<Brand>? _brands;
    private IGenericRepository<Store>? _stores;
    private IGenericRepository<StoreStaff>? _storeStaff;
    private IGenericRepository<Category>? _categories;
    private IGenericRepository<OfferTemplate>? _offerTemplates;
    private IGenericRepository<SaleBatch>? _saleBatches;
    private IGenericRepository<Offer>? _offers;
    private IGenericRepository<Order>? _orders;
    private IGenericRepository<OrderItem>? _orderItems;
    private IGenericRepository<QrTicket>? _qrTickets;
    private IGenericRepository<Payment>? _payments;
    private IGenericRepository<Review>? _reviews;
    private IGenericRepository<Dispute>? _disputes;
    private IGenericRepository<FavoriteStore>? _favoriteStores;
    private IGenericRepository<Notification>? _notifications;

    public UnitOfWork(FreshRescueDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<AppUser> Users => _users ??= new GenericRepository<AppUser>(_context);

    public IGenericRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new GenericRepository<RefreshToken>(_context);

    public IGenericRepository<Brand> Brands => _brands ??= new GenericRepository<Brand>(_context);

    public IGenericRepository<Store> Stores => _stores ??= new GenericRepository<Store>(_context);

    public IGenericRepository<StoreStaff> StoreStaff => _storeStaff ??= new GenericRepository<StoreStaff>(_context);

    public IGenericRepository<Category> Categories => _categories ??= new GenericRepository<Category>(_context);

    public IGenericRepository<OfferTemplate> OfferTemplates => _offerTemplates ??= new GenericRepository<OfferTemplate>(_context);

    public IGenericRepository<SaleBatch> SaleBatches => _saleBatches ??= new GenericRepository<SaleBatch>(_context);

    public IGenericRepository<Offer> Offers => _offers ??= new GenericRepository<Offer>(_context);

    public IGenericRepository<Order> Orders => _orders ??= new GenericRepository<Order>(_context);

    public IGenericRepository<OrderItem> OrderItems => _orderItems ??= new GenericRepository<OrderItem>(_context);

    public IGenericRepository<QrTicket> QrTickets => _qrTickets ??= new GenericRepository<QrTicket>(_context);

    public IGenericRepository<Payment> Payments => _payments ??= new GenericRepository<Payment>(_context);

    public IGenericRepository<Review> Reviews => _reviews ??= new GenericRepository<Review>(_context);

    public IGenericRepository<Dispute> Disputes => _disputes ??= new GenericRepository<Dispute>(_context);

    public IGenericRepository<FavoriteStore> FavoriteStores => _favoriteStores ??= new GenericRepository<FavoriteStore>(_context);

    public IGenericRepository<Notification> Notifications => _notifications ??= new GenericRepository<Notification>(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => _context.Database.BeginTransactionAsync(cancellationToken);

    public void ClearChangeTracker() => _context.ChangeTracker.Clear();
}
