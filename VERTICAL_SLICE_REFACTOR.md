# Refactor STL theo vertical slice (mirror Core1)

Ngày: 2026-06-22. Backup nguyên trạng trước refactor nằm ở `Intern/STL_backup_20260622_025943/`.

> Lưu ý: môi trường thực hiện refactor không có .NET SDK nên **chưa build/test được**.
> Toàn bộ thay đổi đã được kiểm tra tĩnh (namespace, using, reference). Hãy build + chạy migration
> trên máy bạn theo phần "Việc cần làm" bên dưới.

## Đã thay đổi

### 1. Entities gom theo Module (giống Core1)
- `Entities/CatalogModule/` → `Category`, `Product` — namespace `STL.Entities.CatalogModule`
- `Entities/IdentityModule/` → `User`, `UserToken` — namespace `STL.Entities.IdentityModule`
- Mọi `using STL.Entities;` đã đổi sang namespace module tương ứng.

### 2. Constants/ApiRoutes
- Thêm `Constants/ApiRoutes.cs` (nested static, `Base = "/api"`) — gom toàn bộ route.
- 15 endpoint đổi từ `[HttpGet("/api/...")]` sang `[HttpGet(ApiRoutes.X.Y)]` + `using STL.Constants;`.
- Chuỗi route giữ y nguyên → routing không đổi.

### 3. STL.Infrastructure (trước đây rỗng)
Dời phần dùng chung cross-cutting sang đây và nối `ProjectReference` từ `STL.Core`:
- `Models/PagedList.cs`, `Models/PagingRequest.cs` (từ `STL.Models`)
- `Models/Settings/JwtSettings.cs` (từ `STL.Models.Settings`)
- `Interfaces/IHaveSoftDelete.cs`
- `Interceptors/SoftDeleteInterceptor.cs`
- Thêm package `Microsoft.EntityFrameworkCore` + `...Relational` vào `STL.Infrastructure.csproj`.

### 4. Soft-delete end-to-end (mirror cơ chế Core1)
- `Category`, `Product` implement `IHaveSoftDelete`.
- `SoftDeleteInterceptor` đăng ký trong `AddDatabaseServices` (options.AddInterceptors): thao tác
  `Remove` được chuyển thành cập nhật `Deleted = true`.
- `AppDbContext.OnModelCreating` thêm global query filter `!Deleted` cho `Category`, `Product`
  → bản ghi xoá mềm tự động bị ẩn.
- `DeleteCategoryEndpoint` (trước là `Remove` + `Update` lỗi) và `DeleteProductEndpoint` (trước set
  `Deleted` thủ công) đều chuẩn hoá về `dbContext.<Set>.Remove(entity)` để interceptor xử lý.
- Migration snapshot (`AppDbContextModelSnapshot.cs`) đã cập nhật tên type theo namespace mới.

### 5. Dồn dependency về Infrastructure
- Toàn bộ `PackageReference` runtime (AutoMapper, BCrypt, ClosedXML, FastEndpoints, FluentValidation,
  EF Core + Relational, UserSecrets, Npgsql, Sieve, Sylvan, Jwt) chuyển sang `STL.Infrastructure.csproj`.
- `STL.Core` chỉ còn `ProjectReference` tới Infrastructure và nhận các package qua **transitive reference**.
- Ngoại lệ: `Microsoft.EntityFrameworkCore.Design` (tooling, `PrivateAssets=all`) **giữ lại ở Core** vì
  DbContext + Migrations nằm ở đây và `PrivateAssets` không chảy transitively. Muốn Core sạch tuyệt đối
  thì bỏ nó đi và chạy ef với `--startup-project STL/STL.WebApis.csproj` (WebApis đã có Design).
- Đã thêm `AutoMapper 16.1.1` (trước đó thiếu trong mọi csproj dù code đang dùng `IMapper`/`ProjectTo`).

## Việc cần làm (trên máy có .NET 10)

1. Build:
   ```bash
   dotnet build STL.slnx
   ```
2. Soft-delete dùng global query filter — không đổi schema, **không cần migration mới**.
   Nhưng vì đã đổi namespace entity, hãy tạo một migration rỗng để snapshot khớp tuyệt đối:
   ```bash
   dotnet ef migrations add SyncEntityNamespaces --project Domain/STL.Core.csproj
   ```
   Migration này nên rỗng (không có thao tác schema). Nếu nó định drop/tạo lại bảng → dừng lại,
   nghĩa là snapshot chưa khớp; kiểm tra lại `AppDbContextModelSnapshot.cs`.
3. Chạy + smoke test: login, CRUD category/product, xoá (kiểm tra bản ghi vẫn còn trong DB với
   `deleted = true` và không xuất hiện ở GET list).

## Deviation cố ý: chưa áp dụng audit model kiểu Core1

Core1 dùng base `EditableEntity` với **`Guid Id` + 7 cột audit** (`created_date`, `created_by`,
`record_version`...). Entity của STL hiện dùng **`string Id`** và cột audit không đồng nhất
(chỉ Category/Product có `deleted`; Category có `created_at`+`updated_at`; Product chỉ `created_at`;
User/UserToken không có). Bê nguyên `EditableEntity` sẽ đổi kiểu khoá string→Guid và thêm cột mới
→ **vỡ schema + 5 migration hiện có**. Vì vậy phần này **chưa làm**, giữ string key để DB không đổi.

### Nếu muốn mirror cả audit model (làm sau, cần migration)
1. Trong `STL.Infrastructure/Entities/` tạo base + interface:
   - `IAuditableEntity` (CreatedDate, LastUpdatedDate, CreatedBy, ...)
   - `EditableEntity : IAuditableEntity, IHaveSoftDelete` — quyết định `Id` là `Guid` hay giữ `string`.
2. Cho entity kế thừa `EditableEntity`, bỏ field trùng lặp.
3. Thêm `TrackingDbContextInterceptor` tự gán CreatedDate/LastUpdatedDate/CreatedBy (lấy từ `IUserContext`).
4. `dotnet ef migrations add AdoptAuditModel` + review kỹ script (đổi kiểu khoá là thao tác phá huỷ —
   cân nhắc chiến lược dữ liệu nếu DB đã có data).

## Có thể làm thêm để giống Core1 hơn (tuỳ chọn)
- Dời `UserContext`, `AuthConstants`, `UserRoleLevel`, `CustomClaims` sang `STL.Infrastructure`
  (Core1 đặt ở Infrastructure). Hiện giữ ở `STL.Core` để tránh phụ thuộc vòng (circular reference),
  vì muốn dời thì phải dời cả nhóm Auth cùng lúc.
