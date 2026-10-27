# FreshRescue — Table design (generated from the live schema)

> Sinh tự động từ `INFORMATION_SCHEMA` của CSDL `FreshRescue` sau Migration #1 (15/10/2026). Dùng cho mục 1.2.3
> của báo cáo thay cho ảnh chụp *Design* trong SSMS; ảnh *dữ liệu* (≥ 20 bản ghi) chụp sau khi seed xong.
> Enum lưu dạng `tinyint`; giá trị xem `02-DOMAIN-MODEL.md` §3.

17 domain tables (ASP.NET Identity also creates `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`).

## `AspNetUsers` (19 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `FullName` | `nvarchar(100)` | no |  |
| 3 | `AvatarUrl` | `nvarchar(500)` | yes |  |
| 4 | `IsActive` | `bit` | no |  |
| 5 | `CreatedAt` | `datetime2` | no |  |
| 6 | `UserName` | `nvarchar(256)` | yes |  |
| 7 | `NormalizedUserName` | `nvarchar(256)` | yes | UQ |
| 8 | `Email` | `nvarchar(256)` | yes |  |
| 9 | `NormalizedEmail` | `nvarchar(256)` | yes |  |
| 10 | `EmailConfirmed` | `bit` | no |  |
| 11 | `PasswordHash` | `nvarchar(max)` | yes |  |
| 12 | `SecurityStamp` | `nvarchar(max)` | yes |  |
| 13 | `ConcurrencyStamp` | `nvarchar(max)` | yes |  |
| 14 | `PhoneNumber` | `nvarchar(max)` | yes |  |
| 15 | `PhoneNumberConfirmed` | `bit` | no |  |
| 16 | `TwoFactorEnabled` | `bit` | no |  |
| 17 | `LockoutEnd` | `datetimeoffset` | yes |  |
| 18 | `LockoutEnabled` | `bit` | no |  |
| 19 | `AccessFailedCount` | `int` | no |  |

## `RefreshTokens` (6 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `UserId` | `uniqueidentifier` | no | FK?AspNetUsers |
| 3 | `Token` | `nvarchar(200)` | no | UQ |
| 4 | `ExpiresAt` | `datetime2` | no |  |
| 5 | `RevokedAt` | `datetime2` | yes |  |
| 6 | `ReplacedByToken` | `nvarchar(200)` | yes |  |

## `Brands` (14 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `Name` | `nvarchar(150)` | no |  |
| 3 | `Slug` | `nvarchar(160)` | no | UQ |
| 4 | `LogoUrl` | `nvarchar(500)` | yes |  |
| 5 | `Description` | `nvarchar(1000)` | yes |  |
| 6 | `OwnerUserId` | `uniqueidentifier` | no | FK?AspNetUsers, UQ |
| 7 | `Status` | `tinyint` | no |  |
| 8 | `ApprovedAt` | `datetime2` | yes |  |
| 9 | `ApprovedByUserId` | `uniqueidentifier` | yes | FK?AspNetUsers |
| 10 | `StatusReason` | `nvarchar(500)` | yes |  |
| 11 | `IsDeleted` | `bit` | no |  |
| 12 | `DeletedAt` | `datetime2` | yes |  |
| 13 | `CreatedAt` | `datetime2` | no |  |
| 14 | `UpdatedAt` | `datetime2` | yes |  |

## `Stores` (19 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `BrandId` | `uniqueidentifier` | no | FK?Brands |
| 3 | `Name` | `nvarchar(150)` | no |  |
| 4 | `AddressLine` | `nvarchar(300)` | no |  |
| 5 | `Ward` | `nvarchar(100)` | no |  |
| 6 | `District` | `nvarchar(100)` | no |  |
| 7 | `City` | `nvarchar(100)` | no |  |
| 8 | `Latitude` | `decimal(9,6)` | no |  |
| 9 | `Longitude` | `decimal(9,6)` | no |  |
| 10 | `Phone` | `nvarchar(20)` | no |  |
| 11 | `OpenTime` | `time` | no |  |
| 12 | `CloseTime` | `time` | no |  |
| 13 | `Status` | `tinyint` | no |  |
| 14 | `RatingAverage` | `decimal(2,1)` | no |  |
| 15 | `RatingCount` | `int` | no |  |
| 16 | `IsDeleted` | `bit` | no |  |
| 17 | `DeletedAt` | `datetime2` | yes |  |
| 18 | `CreatedAt` | `datetime2` | no |  |
| 19 | `UpdatedAt` | `datetime2` | yes |  |

## `StoreStaff` (7 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `UserId` | `uniqueidentifier` | no | FK?AspNetUsers, UQ |
| 3 | `StoreId` | `uniqueidentifier` | no | FK?Stores |
| 4 | `Position` | `nvarchar(100)` | yes |  |
| 5 | `IsActive` | `bit` | no |  |
| 6 | `CreatedAt` | `datetime2` | no |  |
| 7 | `UpdatedAt` | `datetime2` | yes |  |

## `Categories` (7 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `Name` | `nvarchar(100)` | no |  |
| 3 | `Slug` | `nvarchar(110)` | no | UQ |
| 4 | `IconUrl` | `nvarchar(500)` | yes |  |
| 5 | `DisplayOrder` | `int` | no |  |
| 6 | `IsDeleted` | `bit` | no |  |
| 7 | `DeletedAt` | `datetime2` | yes |  |

## `OfferTemplates` (15 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `BrandId` | `uniqueidentifier` | no | FK?Brands |
| 3 | `CategoryId` | `uniqueidentifier` | no | FK?Categories |
| 4 | `Name` | `nvarchar(200)` | no |  |
| 5 | `Description` | `nvarchar(1000)` | yes |  |
| 6 | `ImageUrl` | `nvarchar(500)` | yes |  |
| 7 | `OfferType` | `tinyint` | no |  |
| 8 | `OriginalPrice` | `decimal(18,0)` | no |  |
| 9 | `SuggestedPrice` | `decimal(18,0)` | no |  |
| 10 | `BrandMinDiscountPercent` | `decimal(5,2)` | no |  |
| 11 | `BrandMaxDiscountPercent` | `decimal(5,2)` | no |  |
| 12 | `IsDeleted` | `bit` | no |  |
| 13 | `DeletedAt` | `datetime2` | yes |  |
| 14 | `CreatedAt` | `datetime2` | no |  |
| 15 | `UpdatedAt` | `datetime2` | yes |  |

## `SaleBatches` (15 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `StoreId` | `uniqueidentifier` | no | FK?Stores |
| 3 | `Name` | `nvarchar(150)` | yes |  |
| 4 | `Status` | `tinyint` | no |  |
| 5 | `SaleStartTime` | `datetime2` | no |  |
| 6 | `SaleEndTime` | `datetime2` | no |  |
| 7 | `PickupStartTime` | `datetime2` | no |  |
| 8 | `PickupEndTime` | `datetime2` | no |  |
| 9 | `CreatedByUserId` | `uniqueidentifier` | no | FK?AspNetUsers |
| 10 | `ActivatedAt` | `datetime2` | yes |  |
| 11 | `ClosedAt` | `datetime2` | yes |  |
| 12 | `TotalUnsoldQuantity` | `int` | yes |  |
| 13 | `CancelReason` | `nvarchar(500)` | yes |  |
| 14 | `CreatedAt` | `datetime2` | no |  |
| 15 | `UpdatedAt` | `datetime2` | yes |  |

## `Offers` (19 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `SaleBatchId` | `uniqueidentifier` | no | FK?SaleBatches |
| 3 | `OfferTemplateId` | `uniqueidentifier` | no | FK?OfferTemplates |
| 4 | `Name` | `nvarchar(200)` | no |  |
| 5 | `Description` | `nvarchar(1000)` | yes |  |
| 6 | `ImageUrl` | `nvarchar(500)` | yes |  |
| 7 | `CategoryId` | `uniqueidentifier` | no | FK?Categories |
| 8 | `OfferType` | `tinyint` | no |  |
| 9 | `OriginalPrice` | `decimal(18,0)` | no |  |
| 10 | `ActualPrice` | `decimal(18,0)` | no |  |
| 11 | `TotalQuantity` | `int` | no |  |
| 12 | `RemainingQuantity` | `int` | no |  |
| 13 | `MaxPerCustomer` | `int` | no |  |
| 14 | `UnsoldQuantity` | `int` | yes |  |
| 15 | `RowVersion` | `timestamp` | no |  |
| 16 | `IsDeleted` | `bit` | no |  |
| 17 | `DeletedAt` | `datetime2` | yes |  |
| 18 | `CreatedAt` | `datetime2` | no |  |
| 19 | `UpdatedAt` | `datetime2` | yes |  |

## `Orders` (19 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `OrderCode` | `char(11)` | no | UQ |
| 3 | `CustomerUserId` | `uniqueidentifier` | no | FK?AspNetUsers |
| 4 | `StoreId` | `uniqueidentifier` | no | FK?Stores |
| 5 | `SaleBatchId` | `uniqueidentifier` | no | FK?SaleBatches |
| 6 | `Status` | `tinyint` | no |  |
| 7 | `CancelledBy` | `tinyint` | yes |  |
| 8 | `CancelReason` | `nvarchar(500)` | yes |  |
| 9 | `RefundStatus` | `tinyint` | no |  |
| 10 | `TotalAmount` | `decimal(18,0)` | no |  |
| 11 | `PlatformFee` | `decimal(18,0)` | no |  |
| 12 | `StorePayoutAmount` | `decimal(18,0)` | no |  |
| 13 | `PaymentDeadline` | `datetime2` | no |  |
| 14 | `PaidAt` | `datetime2` | yes |  |
| 15 | `RedeemedAt` | `datetime2` | yes |  |
| 16 | `CancelledAt` | `datetime2` | yes |  |
| 17 | `RowVersion` | `timestamp` | no |  |
| 18 | `CreatedAt` | `datetime2` | no |  |
| 19 | `UpdatedAt` | `datetime2` | yes |  |

## `OrderItems` (7 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `OrderId` | `uniqueidentifier` | no | FK?Orders |
| 3 | `OfferId` | `uniqueidentifier` | no | FK?Offers |
| 4 | `Quantity` | `int` | no |  |
| 5 | `UnitPrice` | `decimal(18,0)` | no |  |
| 6 | `LineTotal` | `decimal(18,0)` | no |  |
| 7 | `OfferNameSnapshot` | `nvarchar(200)` | no |  |

## `QrTickets` (8 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `OrderId` | `uniqueidentifier` | no | FK?Orders, UQ |
| 3 | `Token` | `nvarchar(500)` | no |  |
| 4 | `IssuedAt` | `datetime2` | no |  |
| 5 | `ExpiresAt` | `datetime2` | no |  |
| 6 | `Status` | `tinyint` | no |  |
| 7 | `UsedAt` | `datetime2` | yes |  |
| 8 | `UsedByStaffUserId` | `uniqueidentifier` | yes | FK?AspNetUsers |

## `Payments` (7 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `OrderId` | `uniqueidentifier` | no | FK?Orders |
| 3 | `Method` | `tinyint` | no |  |
| 4 | `Amount` | `decimal(18,0)` | no |  |
| 5 | `Status` | `tinyint` | no |  |
| 6 | `TransactionRef` | `nvarchar(100)` | yes |  |
| 7 | `CreatedAt` | `datetime2` | no |  |

## `Reviews` (8 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `OrderId` | `uniqueidentifier` | no | FK?Orders, UQ |
| 3 | `CustomerUserId` | `uniqueidentifier` | no | FK?AspNetUsers |
| 4 | `StoreId` | `uniqueidentifier` | no | FK?Stores |
| 5 | `Rating` | `tinyint` | no |  |
| 6 | `Comment` | `nvarchar(1000)` | yes |  |
| 7 | `PhotoUrl` | `nvarchar(500)` | yes |  |
| 8 | `CreatedAt` | `datetime2` | no |  |

## `Disputes` (13 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `OrderId` | `uniqueidentifier` | no | FK?Orders, UQ |
| 3 | `CustomerUserId` | `uniqueidentifier` | no | FK?AspNetUsers |
| 4 | `Reason` | `tinyint` | no |  |
| 5 | `Description` | `nvarchar(1000)` | no |  |
| 6 | `PhotoUrl` | `nvarchar(500)` | no |  |
| 7 | `Status` | `tinyint` | no |  |
| 8 | `MerchantNote` | `nvarchar(1000)` | yes |  |
| 9 | `AdminNote` | `nvarchar(1000)` | yes |  |
| 10 | `ResolvedByUserId` | `uniqueidentifier` | yes | FK?AspNetUsers |
| 11 | `ResolvedAt` | `datetime2` | yes |  |
| 12 | `CreatedAt` | `datetime2` | no |  |
| 13 | `UpdatedAt` | `datetime2` | yes |  |

## `FavoriteStores` (3 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `CustomerUserId` | `uniqueidentifier` | no | PK, FK?AspNetUsers |
| 2 | `StoreId` | `uniqueidentifier` | no | PK, FK?Stores |
| 3 | `CreatedAt` | `datetime2` | no |  |

## `Notifications` (8 columns)

| # | Column | Type | Null | Key |
| --- | --- | --- | --- | --- |
| 1 | `Id` | `uniqueidentifier` | no | PK |
| 2 | `UserId` | `uniqueidentifier` | no | FK?AspNetUsers |
| 3 | `Title` | `nvarchar(200)` | no |  |
| 4 | `Body` | `nvarchar(1000)` | no |  |
| 5 | `Type` | `tinyint` | no |  |
| 6 | `RelatedEntityId` | `uniqueidentifier` | yes |  |
| 7 | `IsRead` | `bit` | no |  |
| 8 | `CreatedAt` | `datetime2` | no |  |
