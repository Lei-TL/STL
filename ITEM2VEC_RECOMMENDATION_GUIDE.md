# Item2Vec Recommendation Guide

## Muc tieu hien tai

He thong da co 2 endpoint de frontend dung ngay:

- `GET /api/products/{id}/you-may-like?limit=10`
- `GET /api/products/{id}/others-also-bought?limit=10`
- `POST /api/recommendations/interactions`
- `POST /api/recommendations/rebuild-fallback`

Hai endpoint lay goi y se uu tien doc bang `product_recommendations`.
Neu bang nay chua co du lieu cho san pham do, he thong tu dong fallback ve san pham cung danh muc.

## Cach endpoint dang hoat dong

### Co the ban thich

Endpoint `you-may-like` lay san pham hien tai, uu tien:

1. Ket qua da co trong bang `product_recommendations` voi type `YouMayLike`.
2. Neu chua co, fallback sang san pham cung danh muc.

Fallback se goi y cac san pham:

1. Cung danh muc voi san pham hien tai.
2. Dang active.
3. Khong phai san pham hien tai.
4. Neu can bo sung thi lay san pham active moi hon.

Day la cach tot de co UI goi y san pham ngay khi chua co du lieu hanh vi.

### Nguoi khac cung mua

Endpoint `others-also-bought` lay:

1. Ket qua da co trong bang `product_recommendations` voi type `OthersAlsoBought`.
2. Neu chua co, fallback sang san pham cung danh muc.

Fallback tra ve reason ro rang:

- `Bought together fallback: same category`
- `Bought together fallback: catalog product`

Khi co du lieu don hang hoac Item2Vec, chi can nap ket qua vao bang `product_recommendations`.
Endpoint khong can doi contract voi frontend.

## Endpoint ghi nhan hanh vi

Dung endpoint:

```text
POST /api/recommendations/interactions
```

Body:

```json
{
  "productId": "P001",
  "sessionId": "session-abc",
  "interactionType": "View",
  "weight": 1
}
```

`interactionType` co cac gia tri:

- `View`
- `SearchClick`
- `AddToCart`
- `Purchase`

Neu khong gui `weight`, backend tu gan:

- `View` = 1
- `SearchClick` = 2
- `AddToCart` = 3
- `Purchase` = 5

Day la nguon du lieu chinh de train Item2Vec.

## Endpoint rebuild fallback

Dung endpoint admin:

```text
POST /api/recommendations/rebuild-fallback
```

Body:

```json
{
  "limitPerProduct": 20
}
```

Endpoint nay xoa ket qua fallback cu va tao lai ket qua fallback vao bang `product_recommendations`.
No giup he thong co san du lieu recommendation trong database truoc khi co model Item2Vec.

## Du lieu can them de lam Item2Vec that

Can them bang ghi nhan hanh vi:

```text
ProductInteractions
- Id
- UserId nullable
- SessionId
- ProductId
- ActionType: View, AddToCart, Purchase
- Weight
- CreatedAt
```

Neu co module don hang thi co the dung them:

```text
Orders
OrderItems
```

Voi Item2Vec, moi session/order/user journey la mot cau, moi product id la mot tu.

Vi du:

```text
session_1: P001 P005 P008
session_2: P005 P002 P001
order_1:   P003 P010 P011
```

## Pipeline Item2Vec de nang cap sau

1. Export interaction/order thanh danh sach sequence.
2. Train Word2Vec bang Python `gensim`.
3. Tinh top N san pham gan nhat cho tung product.
4. Luu vao bang `ProductRecommendations`.
5. Nap ket qua vao bang `ProductRecommendations`.
6. Hai endpoint hien tai se tu doc ket qua moi.

Bang de luu ket qua nen co dang:

```text
ProductRecommendations
- ProductId
- RecommendedProductId
- Score
- RecommendationType: YouMayLike, OthersAlsoBought
- ModelVersion
- CreatedAt
```

## Goi y train Python

```python
from gensim.models import Word2Vec

sentences = [
    ["P001", "P005", "P008"],
    ["P005", "P002", "P001"],
    ["P003", "P010", "P011"],
]

model = Word2Vec(
    sentences=sentences,
    vector_size=64,
    window=5,
    min_count=1,
    sg=1,
    negative=10,
    workers=4,
)

similar = model.wv.most_similar("P001", topn=10)
```

## Cach thay fallback bang ket qua model

Hien tai service da doc bang `ProductRecommendations` truoc.
Khi co model that, chi can insert/update du lieu theo dang:

```text
ProductRecommendations
join Products on RecommendedProductId = Products.Id
where ProductId = id
  and RecommendationType = ...
order by Score desc
```

Neu bang model chua co du lieu cho san pham do, service van giu fallback cung danh muc nhu hien tai.

## Thu tu nen lam tiep

1. Goi `POST /api/recommendations/interactions` khi user xem san pham.
2. Goi endpoint nay tiep cho `AddToCart` va `Purchase` khi co cart/order.
3. Chay `POST /api/recommendations/rebuild-fallback` de co du lieu fallback trong database.
4. Viet job train Item2Vec chay moi dem.
5. Luu top N recommendation vao database voi `ModelVersion` moi.
6. Cache response bang Redis neu endpoint duoc goi nhieu.
