# STL Authen va Author Guide

Muc tieu cua file nay: giup ban hieu va tu trien khai lai luong Authentication va Authorization hien tai trong STL.

Trong file nay:

```text
Authentication = xac thuc: ban la ai?
Authorization  = phan quyen: ban duoc lam gi?
```

STL hien tai dung:

```text
JWT access token
Refresh token luu trong database
BCrypt password hash
Role level: User, Manager, Admin
ASP.NET authorization policy
```

## 1. Tong quan luong auth

Luong chinh:

```text
Register
  -> flow nay dang duoc go de ban tu trien khai lai
  -> khi lam lai can tao user
  -> hash password bang BCrypt
  -> role mac dinh la User

Login
  -> verify password bang BCrypt
  -> tao access token JWT
  -> tao refresh token
  -> luu refresh token vao DB

Call protected endpoint
  -> gui Authorization: Bearer {accessToken}
  -> JWT middleware validate token
  -> Authorization policy check role_level

Refresh
  -> gui accessToken cu + refreshToken
  -> doc principal tu access token het han
  -> check refresh token trong DB
  -> rotate refresh token moi

Logout
  -> xoa refresh token trong DB
  -> refresh token cu khong dung duoc nua
```

## 2. Cac file quan trong

```text
Domain/Entities/User.cs
Domain/Entities/UserToken.cs

Domain/Models/Auth/UserRoleLevel.cs
Domain/Models/Auth/AuthConstants.cs
Domain/Models/Settings/JwtSettings.cs

Domain/SharedServices/Auth/IJwtTokenService.cs
Domain/SharedServices/Auth/JwtTokenService.cs
Domain/SharedServices/UserContext/UserContext.cs

Domain/Endpoints/AuthEndpoints/LoginEnpoint.cs
Domain/Endpoints/AuthEndpoints/RefreshEndpoint.cs
Domain/Endpoints/AuthEndpoints/LogoutEndpoint.cs

STL/Extensions/ServiceCollectionExtensions.cs
STL/Extensions/ApplicationBuilderExtensions.cs
```

Doc theo thu tu nay se de hieu nhat:

```text
1. User, UserToken
2. JwtSettings
3. JwtTokenService
4. Register/Login/Refresh/Logout
5. ServiceCollectionExtensions
6. Product/Category endpoints co Authorize
```

## 3. Entity User

File:

```text
Domain/Entities/User.cs
```

Y tuong:

```csharp
public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRoleLevel RoleLevel { get; set; } = UserRoleLevel.User;
}
```

Luu y:

```text
PasswordHash khong luu password plain text.
PasswordHash luu chuoi BCrypt hash.
RoleLevel quyet dinh user duoc lam gi.
```

Role level hien tai:

```csharp
public enum UserRoleLevel
{
    User = 1,
    Manager = 2,
    Admin = 3
}
```

Dung so tang dan de policy co the check:

```text
role_level >= minimumRoleLevel
```

Nghia la:

```text
Admin = 3 se qua duoc policy Manager = 2 va User = 1.
Manager = 2 se qua duoc User = 1 nhung khong qua Admin = 3.
User = 1 chi qua duoc User = 1.
```

## 4. Entity UserToken

File:

```text
Domain/Entities/UserToken.cs
```

Y tuong:

```csharp
public class UserToken
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiryTime { get; set; }
}
```

Tai sao refresh token nam trong DB?

```text
De co the huy refresh token.
De logout that su lam refresh token cu vo hieu.
De rotate refresh token moi moi lan refresh.
```

Access token la stateless. Server khong luu access token.

Refresh token la stateful. Server luu refresh token trong DB.

## 5. Password hash bang BCrypt

Khi register:

```csharp
PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
```

Khi login:

```csharp
BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)
```

Vi sao dung BCrypt?

```text
BCrypt tu tao salt.
BCrypt cham co chu dich, kho brute-force hon hash nhanh.
Khong can tu quan ly salt rieng.
```

Quan trong:

```text
DB khong luu password goc.
DB chi luu BCrypt hash dang $2a$... hoac $2b$...
```

Neu password cu trong DB la hash cua ASP.NET Identity thi sau khi doi sang BCrypt, user cu se khong login duoc. Can reset password hoac migrate hash.

## 6. JWT access token

JWT duoc tao trong:

```text
Domain/SharedServices/Auth/JwtTokenService.cs
```

Method chinh:

```csharp
string GenerateAccessToken(IEnumerable<Claim> claims, DateTime expires);
```

Khi login, STL tao claims:

```csharp
new(ClaimTypes.Name, user.Email)
new(ClaimTypes.Email, user.Email)
new(ClaimTypes.NameIdentifier, user.Id)
new(ClaimTypes.Sid, user.Id)
new(ClaimTypes.Role, user.RoleLevel.ToString())
new(AuthConstants.RoleLevelClaim, ((int)user.RoleLevel).ToString())
```

Claim quan trong nhat cho authorization la:

```text
role_level
```

Vi policy hien tai check `role_level`, khong check truc tiep string role.

## 7. JwtSettings

Config nam trong:

```text
STL/appsettings.json
```

Dang:

```json
{
  "JwtSettings": {
    "Issuer": "STL",
    "Audience": "STL.Client",
    "SigningKey": "..."
  }
}
```

Y nghia:

```text
Issuer     = ai phat hanh token
Audience   = token danh cho client nao
SigningKey = secret de ky token
```

Khi validate token, app check:

```csharp
ValidateIssuer = true
ValidateAudience = true
ValidateLifetime = true
ValidateIssuerSigningKey = true
```

Nghia la token phai:

```text
dung issuer
dung audience
chua het han
co chu ky hop le
```

## 8. Register endpoint

Trang thai hien tai:

```text
RegisterEndpoint da duoc go ra khoi code de ban tu trien khai lai tu dau.
```

Route:

```text
POST /api/auth/register
```

Cho phep anonymous:

```csharp
[AllowAnonymous]
```

Input:

```json
{
  "email": "user1@example.com",
  "password": "Pass123!"
}
```

Luong:

```text
1. Trim va lowercase email.
2. Check email da ton tai chua.
3. Hash password bang BCrypt.
4. Tao user role User.
5. Luu DB.
6. Tao access token + refresh token.
7. Luu refresh token vao DB.
8. Tra ve token.
```

Vi sao user moi khong duoc chon role?

```text
Neu register cho client truyen role, ai cung co the tu tao Admin.
```

Muon tao Manager/Admin de test thi update DB:

```sql
update users set role_level = 2 where email = 'manager@example.com';
update users set role_level = 3 where email = 'admin@example.com';
```

## 9. Login endpoint

File:

```text
Domain/Endpoints/AuthEndpoints/LoginEnpoint.cs
```

Route:

```text
POST /api/auth/login
```

Input:

```json
{
  "email": "user1@example.com",
  "password": "Pass123!"
}
```

Luong:

```text
1. Tim user theo email.
2. Verify password bang BCrypt.
3. Tao claims gom user id, email, role, role_level.
4. Tao access token het han sau 30 phut.
5. Tao refresh token het han sau 7 ngay.
6. Luu refresh token vao user_tokens.
7. Tra access token + refresh token.
```

Response:

```json
{
  "accessToken": "...",
  "accessTokenExpireAt": "...",
  "refreshToken": "...",
  "refreshTokenExpireAt": "..."
}
```

Neu login lai, refresh token cu cua user se bi thay bang refresh token moi.

## 10. Refresh endpoint

File:

```text
Domain/Endpoints/AuthEndpoints/RefreshEndpoint.cs
```

Route:

```text
POST /api/auth/refresh
```

Cho phep anonymous vi luc refresh, access token co the da het han.

Input:

```json
{
  "accessToken": "old-access-token",
  "refreshToken": "current-refresh-token"
}
```

Luong:

```text
1. Doc principal tu access token cu, bo qua lifetime.
2. Lay userId trong token.
3. Tim user trong DB.
4. Tim refresh token trong DB theo userId.
5. Check refresh token trong request co khop DB khong.
6. Check refresh token con han khong.
7. Tao access token moi.
8. Tao refresh token moi.
9. Update refresh token trong DB.
10. Tra token moi.
```

Day goi la refresh token rotation.

Tai sao phai rotate?

```text
Neu refresh token cu bi lo, sau mot lan refresh thanh cong, token cu se khong dung duoc nua.
```

## 11. Logout endpoint

File:

```text
Domain/Endpoints/AuthEndpoints/LogoutEndpoint.cs
```

Route:

```text
POST /api/auth/logout
```

Can token User tro len:

```csharp
[Authorize(Policy = AuthConstants.Policies.User)]
```

Luong:

```text
1. Lay userId tu access token hien tai.
2. Tim user token trong DB.
3. Xoa refresh token trong DB.
4. Tra 200 OK.
```

Sau logout:

```text
Access token hien tai co the van song den khi het han.
Refresh token khong con dung duoc.
```

Day la han che binh thuong cua JWT stateless. Neu muon access token bi huy ngay, can them blacklist/token version/session table.

## 12. Authentication DI

File:

```text
STL/Extensions/ServiceCollectionExtensions.cs
```

Dang ky:

```csharp
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);
```

Y nghia:

```text
Day app cach doc Authorization: Bearer {token}
Day app cach validate issuer/audience/lifetime/signing key
```

Pipeline:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapFastEndpoints();
```

Thu tu quan trong:

```text
UseAuthentication truoc UseAuthorization.
MapFastEndpoints sau auth.
```

Neu thieu `UseAuthentication`, token se khong duoc doc.

Neu thieu `UseAuthorization`, endpoint co `[Authorize]` se khong chan dung cach.

## 13. Authorization policies

Policies nam trong:

```text
STL/Extensions/ServiceCollectionExtensions.cs
```

Hien co 3 policy:

```text
UserLevel
ManagerLevel
AdminLevel
```

Dang ky bang:

```csharp
options.AddPolicy(
    AuthConstants.Policies.Manager,
    policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
            HasMinimumRoleLevel(context.User, UserRoleLevel.Manager)));
```

Ham check:

```csharp
private static bool HasMinimumRoleLevel(
    ClaimsPrincipal user,
    UserRoleLevel minimumRoleLevel)
{
    var roleLevelClaim = user.FindFirst(AuthConstants.RoleLevelClaim);

    return roleLevelClaim is not null
        && int.TryParse(roleLevelClaim.Value, out var roleLevel)
        && roleLevel >= (int)minimumRoleLevel;
}
```

Y tuong:

```text
Endpoint can Manager thi Manager va Admin vao duoc.
Endpoint can Admin thi chi Admin vao duoc.
Endpoint can User thi User, Manager, Admin deu vao duoc.
```

## 14. Rule quyen hien tai

Auth endpoints:

```text
POST /api/auth/register  anonymous
POST /api/auth/login     anonymous
POST /api/auth/refresh   anonymous
POST /api/auth/logout    User tro len
```

Category/Product endpoints:

```text
User tro len:
GET /api/products
GET /api/product/{id}
GET /api/categories
GET /api/category/{id}

Manager tro len:
POST /api/products
PUT /api/products
POST /api/category
PUT /api/category

Admin:
DELETE /api/products/{id}
DELETE /api/category/{id}
```

## 15. Test bang Postman

### 15.1. Register

```text
POST {{url}}/api/auth/register
Content-Type: application/json
```

Body:

```json
{
  "email": "user1@example.com",
  "password": "Pass123!"
}
```

Ky vong:

```text
200 OK
```

### 15.2. Login

```text
POST {{url}}/api/auth/login
Content-Type: application/json
```

Body:

```json
{
  "email": "user1@example.com",
  "password": "Pass123!"
}
```

Copy:

```text
accessToken
refreshToken
```

### 15.3. Goi endpoint can auth

Vi du:

```text
GET {{url}}/api/categories?PageNumber=1&PageSize=10
```

Header:

```text
Authorization: Bearer {{accessToken}}
```

Neu khong gui token:

```text
401 Unauthorized
```

Neu co token nhung role thap:

```text
403 Forbidden
```

### 15.4. Test User khong duoc create

User moi register co role `User = 1`.

Goi:

```text
POST {{url}}/api/category
Authorization: Bearer {{accessToken}}
```

Body:

```json
{
  "name": "Test Category"
}
```

Ky vong:

```text
403 Forbidden
```

### 15.5. Nang user thanh Manager

Chay SQL:

```sql
update users set role_level = 2 where email = 'user1@example.com';
```

Login lai de lay token moi.

Sau do:

```text
POST /api/category
PUT /api/category
```

Ky vong:

```text
200 OK
```

Nhung:

```text
DELETE /api/category/{id}
```

van phai la:

```text
403 Forbidden
```

### 15.6. Nang user thanh Admin

Chay SQL:

```sql
update users set role_level = 3 where email = 'user1@example.com';
```

Login lai de lay token moi.

Sau do:

```text
DELETE /api/category/{id}
```

Ky vong:

```text
200 OK
```

### 15.7. Refresh token

```text
POST {{url}}/api/auth/refresh
Content-Type: application/json
```

Body:

```json
{
  "accessToken": "{{oldAccessToken}}",
  "refreshToken": "{{refreshToken}}"
}
```

Ky vong:

```text
200 OK
```

Response se co access token moi va refresh token moi.

Refresh token cu bi rotate, khong nen dung lai.

### 15.8. Logout

```text
POST {{url}}/api/auth/logout
Authorization: Bearer {{accessToken}}
```

Ky vong:

```text
200 OK
```

Sau logout, goi refresh bang refresh token cu:

```text
401 Unauthorized
```

## 16. Ket qua test gan nhat

Flow da duoc test qua API that voi Postgres truoc khi go RegisterEndpoint:

```text
REGISTER status=200
LOGIN_USER status=200
USER_GET_CATEGORIES status=200
USER_CREATE_CATEGORY status=403
LOGIN_MANAGER status=200
MANAGER_CREATE_CATEGORY status=200
MANAGER_UPDATE_CATEGORY status=200
MANAGER_DELETE_CATEGORY status=403
LOGIN_ADMIN status=200
ADMIN_DELETE_CATEGORY status=200
REFRESH status=200
LOGOUT status=200
REFRESH_AFTER_LOGOUT status=401
```

Dieu nay xac nhan thiet ke auth da chay dung. Sau khi RegisterEndpoint bi go, can trien khai lai register roi test lai flow tu dau.

```text
Authentication chay duoc.
JWT token tao va validate duoc.
Role-level authorization chay dung.
Refresh token co rotate.
Logout huy refresh token thanh cong.
```

## 17. Loi de gap

### 17.1. 401 Unauthorized

Thuong la:

```text
Khong gui Authorization header.
Gui sai format Bearer token.
Token het han.
SigningKey/Issuer/Audience khong khop.
```

Header dung:

```text
Authorization: Bearer eyJ...
```

### 17.2. 403 Forbidden

Thuong la:

```text
Token hop le nhung role_level khong du.
```

Vi du User goi endpoint Manager/Admin.

### 17.3. Doi role trong DB nhung token van role cu

JWT la token da ky. Claim role nam trong token.

Neu update DB:

```sql
update users set role_level = 3 where email = 'user1@example.com';
```

thi phai login lai de lay access token moi.

### 17.4. Refresh sau logout bi 401

Day la dung.

Logout xoa refresh token trong DB, nen refresh token cu khong con hop le.

### 17.5. Redis warning khi test

Neu Redis khong chay, endpoint list/create/update/delete co the log warning. Code cache hien tai co fallback nen API van co the chay.

Bat Redis de log sach hon:

```powershell
docker compose up -d redis
```

### 17.6. Postgres schema

Moi truong hien tai dang dung:

```text
Database: ecomerce
User: postgres
Schema: ecommerce
```

Connection string:

```text
Host=localhost;Port=5432;Database=ecommerce;Username=postgres;Password=147852369
```

Neu thieu `Search Path=ecommerce`, EF co the bao:

```text
no schema has been selected to create in
```

## 18. Nen cai tien gi tiep?

Nhung diem co the lam sau:

```text
1. Them endpoint Admin de gan role thay vi update SQL truc tiep.
2. Hash refresh token trong DB thay vi luu plain refresh token.
3. Them access token blacklist hoac token version neu can logout huy access token ngay.
4. Them seed admin dau tien.
5. Them integration tests that su cho auth flow.
6. Rut ngan timeout Redis khi Redis khong chay.
```

Hien tai flow da du cho muc tieu hoc va demo:

```text
Register -> Login -> Role policy -> Refresh -> Logout
```
