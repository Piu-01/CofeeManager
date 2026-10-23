# Contract sản phẩm/công thức — migration005 / T10

ADMIN quản trị; CASH đọc menu bán, STORE đọc menu và công thức. BLL recheck phiên;
SQL guard actor/stamp đọc role DB. Product Code SP001–999, Name≤50, Category FK,
Price nguyênVND>0, Status Dang ban/Ngung ban; mã bất biến. RecipeLineInput nhận mã
rộng và lượng decimal38,18, kiểm trùng/FK/scale4 trước cast decimal18,4.

Create kèm recipe có thể bật bán nguyên tử; UI tạo Ngừng bán rồi editor công thức
và bật bán. Update metadata giữ recipe. ReplaceRecipe thay toàn tập, bump revision
sản phẩm, receipt+revision+công thức cùng transaction. Không để món đang bán thiếu
recipe; ngừng bán không bỏ dòng đơn mở. ProductList selector chỉ trả bán+córecipe.
Giá CHITIET_HD và tiêu hao cũ không thay khi đổi menu/recipe.

sp_ProductCreate/Update/Delete/ReplaceRecipe; sp_ProductList/Get, sp_RecipeList.
List paging50, filter tên/mã/danh mục/bán; write ExpectedRevision/RequestId, kết quả
Code/Revision. Receipt/reconcile theo material-contract. Runtime DENY DML.

Giao thức khóa: writer sản phẩm/recipe Exclusive `CafeManager:Recipes`, checkout
baseline Shared trước header; giữ transaction-owned đến commit. Migration008 bổ
sung Policy Shared trước Recipes. T15 phải kế thừa thứ tự Policy→Recipes→Header.
005 chỉ bổ sung khóa baseline, chưa thay tính ưu đãi/checkout đích.

SQL technical clean/demo/smoke tại G03/runs/technical_20261018_193133_4199956e;
2DB tạo/dọn, không còn giữ. Lượt trước sai alias SANPHAM đã rollback/dọn1DB.
Nghiệm thu snapshot/concurrency/baseline và tích hợp G04 chưa chạy.
