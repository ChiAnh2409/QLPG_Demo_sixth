using QLPG.Models;
using QLPG.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace QLPG.Controllers
{
    public class HoiVienController : Controller
    {
        private QLPG1Entities db = new QLPG1Entities();
        //tạo biến database để lấy dữ liệu
        // GET: HoiVien
        public ActionResult HoiVien()
        {
            var list = new MultipleData();
            list.hoiViens = db.HoiVien.Include("ThanhVien").ToList();
            //tình trạng gói tập của hội viên
            foreach (var hoiVien in list.hoiViens)
            {
                var goiTap = db.ChiTietDK_GoiTap
                                .Where(ct => ct.id_HV == hoiVien.id_HV)
                                .OrderByDescending(ct => ct.NgayKetThuc)
                                .FirstOrDefault();

                if (goiTap != null && goiTap.NgayKetThuc >= DateTime.Now)
                {
                    hoiVien.TinhTrang = true;
                }
                else
                {
                    hoiVien.TinhTrang = false;
                }

                // Cập nhật trạng thái của hội viên trong cơ sở dữ liệu
                db.Entry(hoiVien).State = EntityState.Modified;
            }

            // Lưu các thay đổi vào cơ sở dữ liệu
            db.SaveChanges();

            list.vien = db.ThanhVien.ToList();
            return View(list);
        }
        public ActionResult ThemHV() 
        {
            // Lấy danh sách thành viên chưa là hội viên
            var chuaLaHoiVienIds = db.HoiVien.Select(hv => hv.id_TV).ToList();
            var chuaLaHoiVien = db.ThanhVien.Where(tv => !chuaLaHoiVienIds.Contains(tv.id_TV)).ToList();

            var list = new MultipleData
            {
                hoiViens = db.HoiVien.Include("ThanhVien"),
                vien = chuaLaHoiVien
            };
            return View(list);
        }
        [HttpPost]
        public ActionResult ThemHV(HoiVien hv)
        {
            String HinhAnh = "";

            HttpPostedFileBase file = Request.Files["HinhAnh"];
            if (file != null && file.FileName != "")
            {
                String serverPath = HttpContext.Server.MapPath("~/assets/img/team");
                String filePath = serverPath + "/" + file.FileName;
                file.SaveAs(filePath);
                HinhAnh = file.FileName;
            }
            hv.HinhAnh = HinhAnh;
            db.HoiVien.Add(hv);
            hv.TinhTrang = true;
            DateTime now = DateTime.Now;
            hv.NgayGiaNhap = now;
            db.SaveChanges();
            return RedirectToAction("HoiVien");
        }
        public ActionResult SuaHV(int id)
        {
            // Lấy thông tin hội viên hiện tại từ CSDL
            var existingHoiVien = db.HoiVien.Find(id);

            // Lấy danh sách thành viên chưa là hội viên
            var chuaLaHoiVienIds = db.HoiVien.Select(hv => hv.id_TV).ToList();
            var chuaLaHoiVien = db.ThanhVien.Where(tv => !chuaLaHoiVienIds.Contains(tv.id_TV)).ToList();

            var viewmodel = new MultipleData
            {
                hoiViens = existingHoiVien != null ? new List<HoiVien> { existingHoiVien } : new List<HoiVien>(),
                vien = chuaLaHoiVien
            };

            return View(viewmodel);
        }
        [HttpPost]
        public ActionResult SuaHV(HoiVien hv)
        {
            HoiVien existingHoiVien = db.HoiVien.Find(hv.id_HV);
            if (existingHoiVien != null)
            {
                existingHoiVien.id_TV = hv.id_TV;
                existingHoiVien.NgaySinh = hv.NgaySinh;
                existingHoiVien.CCCD = hv.CCCD;
                existingHoiVien.TinhTrang = hv.TinhTrang;

                // Kiểm tra và lưu hình ảnh nếu có
                HttpPostedFileBase file = Request.Files["HinhAnh"];
                if (file != null && file.FileName != "")
                {
                    String HinhAnh = file.FileName;
                    String serverPath = HttpContext.Server.MapPath("~/assets/img/team");
                    String filePath = serverPath + "/" + HinhAnh;
                    file.SaveAs(filePath);
                    existingHoiVien.HinhAnh = HinhAnh;
                }

                db.SaveChanges();
            }

            return RedirectToAction("HoiVien");
        }
        public ActionResult XoaHV(int id)
        {
            var HoiVien = db.HoiVien.Find(id);
            if (HoiVien != null)
            {
                db.HoiVien.Remove(HoiVien);
                db.SaveChanges();

            }
            return RedirectToAction("HoiVien");
        }
        [HttpPost] //Tìm kiếm bằng tên trong bảng hội viên nhưng tham chiếu bằng id_TV trong bảng thành viên
        public ActionResult TimKiemHV(string search)
        {
            var list = new MultipleData();
            list.hoiViens = db.HoiVien.Include("ThanhVien").Where(hv => hv.ThanhVien.TenTV.Contains(search)).ToList();
            list.vien = db.ThanhVien.ToList();

            return View("HoiVien", list);
        }

        // Điểm danh cho hội viên
        [HttpGet]
        public ActionResult DiemDanhHV(int id_HV)
        {
            var hoiVien = db.HoiVien.Include("ThanhVien").FirstOrDefault(hv => hv.id_HV == id_HV);

            if (hoiVien != null)
            {
                return View(hoiVien);
            }
            // Kiểm tra xem có thông báo điểm danh thành công không
            if (TempData["DiemDanhSuccess"] != null)
            {
                ViewBag.DiemDanhSuccess = TempData["DiemDanhSuccess"].ToString();
            }
            return HttpNotFound();
        }

        [HttpPost]
        public ActionResult DiemDanhHV(int id_HV, bool DaDiemDanh)
        {
            var hoiVien = db.HoiVien.Find(id_HV);
            if (hoiVien != null)
            {
                // Kiểm tra ModelState.IsValid
                if (ModelState.IsValid)
                {
                    // Lấy ngày hiện tại
                    var currentDate = DateTime.Now.Date;

                    // Tạo biến tạm để lưu ngày kết thúc của ngày hiện tại
                    var nextDate = currentDate.AddDays(1);

                    // Lấy buổi tập của hội viên trong ngày
                    var buoiTapTrongNgay = db.BuoiTap
                        .Where(bt => bt.id_HV == id_HV && bt.NgayThamGia.HasValue && bt.NgayThamGia.Value >= currentDate && bt.NgayThamGia.Value < nextDate)
                        .FirstOrDefault();

                    if (buoiTapTrongNgay == null)
                    {
                        // Hội viên chưa điểm danh trong ngày, thêm mới buổi tập
                        var buoiTap = new BuoiTap
                        {
                            id_HV = id_HV,
                            DaDiemDanh = DaDiemDanh,
                            NgayThamGia = currentDate
                        };

                        db.BuoiTap.Add(buoiTap);
                        db.SaveChanges();

                        // Thông báo đã điểm danh thành công
                        TempData["DiemDanhSuccess"] = "Đã điểm danh thành công.";

                        // Chuyển hướng về trang DiemDanhHV với tham số id_HV
                        return RedirectToAction("DiemDanhHV", new { id_HV = id_HV });
                    }
                    else
                    {
                        // Thông báo rằng hội viên đã điểm danh trong ngày
                        ModelState.AddModelError("", "Hội viên đã điểm danh trong ngày.");
                    }
                }
            }

            return HttpNotFound();
        }

        // Xem chi tiết điểm danh
        public ActionResult CTDiemDanh(int id_HV, DateTime ngay)
        {
            var list = new MultipleData();
            list.hoiViens = db.HoiVien.Include("ThanhVien").ToList();
            list.vien = db.ThanhVien.ToList();

            // Lấy thông tin hội viên
            var hoiVien = db.HoiVien.Include("ThanhVien").FirstOrDefault(hv => hv.id_HV == id_HV);

            // Kiểm tra xem hội viên đã điểm danh trong ngày chưa
            var daDiemDanhTrongNgay = db.BuoiTap
                .Any(bt => bt.id_HV == id_HV && bt.NgayThamGia.HasValue &&
                           bt.NgayThamGia.Value.Year == ngay.Year &&
                           bt.NgayThamGia.Value.Month == ngay.Month &&
                           bt.NgayThamGia.Value.Day == ngay.Day);

            if (daDiemDanhTrongNgay)
            {
                // Lấy danh sách buổi tập của hội viên trong ngày cụ thể
                var buoiTaps = db.BuoiTap
                    .Where(bt => bt.id_HV == id_HV && bt.NgayThamGia.HasValue &&
                           bt.NgayThamGia.Value.Year == ngay.Year &&
                           bt.NgayThamGia.Value.Month == ngay.Month &&
                           bt.NgayThamGia.Value.Day == ngay.Day)
                    .ToList();

                // Lọc danh sách buổi tập theo thời gian và chỉ giữ lại một bản ghi cho mỗi thời gian
                var distinctBuoiTaps = buoiTaps
                    .GroupBy(bt => bt.NgayThamGia)
                    .Select(group => group.First())
                    .ToList();

                ViewBag.TenHoiVien = hoiVien.ThanhVien.TenTV;  // Truyền tên hội viên vào ViewBag
                list.buoiTap = distinctBuoiTaps;
            }
            else
            {
                ViewBag.TenHoiVien = hoiVien.ThanhVien.TenTV;
                list.buoiTap = new List<BuoiTap>(); // Không có buổi tập nào nếu chưa điểm danh
            }

            return View(list);
        }
    }
}