﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Web_Sach.Models;
using Web_Sach.Models.EF;
using Web_Sach.Session;
using System.Web.Script.Serialization;
using System.Configuration;
using Web_Sach.Models.Payments;
using System.Diagnostics;
using System.Text;
using BotDetect;
using System.Web.Services.Description;
using System.Runtime.CompilerServices;
using System.Data.Entity;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;


namespace Web_Sach.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        private WebSachDb db = new WebSachDb();
        public ActionResult Index()
        {

            if (Session[SessionHelper.USER_KEY] == null)
            {// user chưa tồn tại
                Session[SessionHelper.CART_KEY] = null;
                return RedirectToAction("Cart", "Cart");

            }
            // user đã tồn tại
            var cart = Session[SessionHelper.CART_KEY];
            var list = new List<CartItem>();
            if (cart != null)
            {
                list = (List<CartItem>)cart;
            }
            return View(list);
        }
        public ActionResult AddCart(int productId, int Quantity)
        {
            if (Session[SessionHelper.USER_KEY] == null)
            {// user chưa tồn tại
                Session[SessionHelper.CART_KEY] = null;
                return Json(new { status = false, role = 0 });

            }
            var cart = Session[SessionHelper.CART_KEY]; // lấy giỏ hàng
            var list = new List<CartItem>();
            // lấy 1 đối tượng sách
            var sach = from s in db.Saches
                       where s.ID == productId
                       select s;

            if (cart != null)
            {// đã tồn tại giỏ hàng
                list = (List<CartItem>)cart;
                // kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
                if (list.Exists(x => x.sach.ID == productId))
                { // true khi có id sản phẩm giống nhau
                    foreach (var item in list)
                    {
                        if (item.sach.ID == productId)
                        {
                            item.Quantity += Quantity;
                        }
                    }

                }
                else
                { // sản phẩm đó chưa có trong giỏ  hàng
                    var item = new CartItem();
                    item.sach = sach.FirstOrDefault();
                    int? sale = 0;

                    var khuyenMai = sach.FirstOrDefault().KhuyenMai_Sach.ToList();
                    if (khuyenMai.Count > 0)
                    {
                        foreach (var itemKM in khuyenMai)
                        {// lặp qua danh sách khuyến mại
                            if (DateTime.Now >= itemKM.KhuyenMai.NgayBatDau && DateTime.Now <= itemKM.KhuyenMai.NgayKeThuc)
                            {
                                sale = itemKM.Sale;
                                break;
                            }
                        }
                    }
                    item.sach.Price = (int)item.sach.Price - (item.sach.Price * sale / 100);
                    item.Quantity = Quantity;
                    list.Add(item);

                }

                Session[SessionHelper.CART_KEY] = list;

            }
            else
            { // chưa tồn tại giỏ hàng
                /// list = (List<CartItem>)cart; 
                var item = new CartItem();
                item.sach = sach.FirstOrDefault();
                int? sale = 0;
                var khuyenMai = sach.FirstOrDefault().KhuyenMai_Sach.ToList();
                if (khuyenMai.Count > 0)
                {
                    foreach (var itemKM in khuyenMai)
                    {// lặp qua danh sách khuyến mại
                        if (DateTime.Now >= itemKM.KhuyenMai.NgayBatDau && DateTime.Now <= itemKM.KhuyenMai.NgayKeThuc)
                        {
                            sale = itemKM.Sale;
                            break;
                        }
                    }
                }

                item.sach.Price = (int)item.sach.Price - (item.sach.Price * sale / 100);
                item.Quantity = Quantity;

                // var list = new List<CartItem>();
                list.Add(item);
                Session[SessionHelper.CART_KEY] = list;
            }
            return Json(new { status = true });
            //return RedirectToAction("Index");
        }
        // đăng nhập mới cho thêm vào Cart
        [HttpGet]
        public ActionResult GetCartCount()
        {
            var cart = Session[SessionHelper.CART_KEY] as List<CartItem>;
            return Content(cart.Count().ToString());
        }
        public ActionResult Cart()
        {
            return View();
        }
        [HttpPost]
        public JsonResult Update(string cartList)
        {
            // thêm không gian system.web.script.serialization
            // Lớp này được sử dụng để chuyển đổi chuỗi JSON thành các đối tượng C#
            var jsonList = new JavaScriptSerializer().Deserialize<List<CartItem>>(cartList);
            var sessionCart = (List<CartItem>)Session[SessionHelper.CART_KEY];
            foreach (var item in sessionCart)
            {
                var jsonItem = jsonList.SingleOrDefault(x => x.sach.ID == item.sach.ID);
                if (jsonItem != null)
                {
                    item.Quantity = jsonItem.Quantity;
                }

            }
            Session[SessionHelper.CART_KEY] = sessionCart;
            return Json(new
            {
                status = true

            });
        }
        [HttpPost]
        public JsonResult DeleteAll()
        {
            Session[SessionHelper.CART_KEY] = null;
            return Json(new
            {
                status = true
            });
        }
        [HttpPost]
        public JsonResult Delete(int id)
        {
            var cart = Session[SessionHelper.CART_KEY] as List<CartItem>;
            cart.RemoveAll(x => x.sach.ID == id);
            Session[SessionHelper.CART_KEY] = cart;
            return Json(new
            {
                status = true
            });
        }
        [HttpGet]
        public ActionResult BuyNow(int id, int Quantity)
        {
            var sessionUser = (UserLoginSession)Session[SessionHelper.USER_KEY];
            if (sessionUser != null)
            {
                ViewBag.user = sessionUser;
                var item = new CartItem();
                item.sach = new Book().GetBookById(id);
                item.Quantity = Quantity;
                return View(item);
            }
            return RedirectToAction("Cart", "Cart"); // hiện thi về trang đăng nhập


        }
        [HttpPost]
        public JsonResult BuyNowPost(string TenKH, string Mobile, string Address, string Email, int total, int idSach, int Quantity, int PriceSale, string mavoucher, string TypePayment, string TypePaymentVN)
        {

            //string TenKH, string Mobile, string Address, string Email, int total, int id, int Quantity, int PriceSale
            var code = new { Success = false, Code = 1, Url = "", DonGia = 0 };
            var sessionUser = (UserLoginSession)Session[SessionHelper.USER_KEY];
            var order = new DonHang();
            var tienGiam = decimal.Zero;
            if (!string.IsNullOrEmpty(mavoucher))
            {// mã voucher 
                var dbVoucher = db.Vouchers.Where(x => x.MaVoucher == mavoucher).FirstOrDefault();
                order.TongTien = total - (decimal)dbVoucher.SoTienGiam;
                tienGiam = (decimal)dbVoucher.SoTienGiam;
                order.MaVoucher = dbVoucher.ID;
                dbVoucher.SoLanDaSuDung = dbVoucher.SoLanDaSuDung + 1;
                db.Vouchers.Attach(dbVoucher);
                db.Entry(dbVoucher).Property(s => s.SoLanDaSuDung).IsModified = true;
                // db.SaveChanges();

            }
            else
            {
                order.TongTien = total;
                order.MaVoucher = null;
            }
            order.TenNguoiNhan = TenKH;
            order.Moblie = Mobile;
            order.DiaChiNguoiNhan = Address;
            order.Email = Email;
            order.NgayDat = DateTime.Now;
            order.DaThanhToan = 0;
            order.MaKH = sessionUser.UserID;
            order.Status = 1;
            order.ChiTietDonHangs.Add(new ChiTietDonHang
            {
                MaSach = idSach,
                Quantity = Quantity,
                Price = PriceSale,
            });
            db.DonHangs.Add(order);
            var idOrder = order.ID;
            var sach = db.Saches.Find(idSach);
            // cập nhật lại số lượng
            sach.Quantity = sach.Quantity - Quantity;
            db.Entry(sach).Property(s => s.Quantity).IsModified = true;
            db.SaveChanges();
            int type = int.Parse(TypePayment);
            code = new { Success = true, Code = type, Url = "", DonGia = 0 };
            //send mail
            var strSanPham = "";
            var thanhtien = decimal.Zero;
            var TongTien = decimal.Zero;

            strSanPham += "<tr>";
            strSanPham += "<td>" + db.Saches.Find(idSach).Name + "</td>";
            strSanPham += "<td>" + Quantity + "</td>";
            strSanPham += "<td>" + (PriceSale.ToString("N0")) + "</td>";
            strSanPham += "</tr>";


            thanhtien = total;
            TongTien = thanhtien - tienGiam;

            string contentCustomer = System.IO.File.ReadAllText(Server.MapPath("~/TemplateEmail/send2.html"));
            contentCustomer = contentCustomer.Replace("{{MaDH}}", idOrder.ToString());
            contentCustomer = contentCustomer.Replace("{{SanPham}}", strSanPham);
            contentCustomer = contentCustomer.Replace("{{NgayDatHang}}", DateTime.Now.ToString("dd-MM-yyyy"));
            contentCustomer = contentCustomer.Replace("{{TenKhachHang}}", TenKH);
            contentCustomer = contentCustomer.Replace("{{Phone}}", Mobile);
            contentCustomer = contentCustomer.Replace("{{Email}}", Email);
            if (type == 2)
            {
                contentCustomer = contentCustomer.Replace("{{Payments}}", "Chuyển khoản ngân hàng");
            }
            else if (type == 1)
            {
                contentCustomer = contentCustomer.Replace("{{Payments}}", "Thanh toán khi nhận hàng");
            }
            contentCustomer = contentCustomer.Replace("{{TienGiam}}", tienGiam.ToString("N0"));
            contentCustomer = contentCustomer.Replace("{{Address}}", Address);
            contentCustomer = contentCustomer.Replace("{{ThanhTien}}", thanhtien.ToString("N0"));
            contentCustomer = contentCustomer.Replace("{{TongTien}}", TongTien.ToString("N0"));
            Web_Sach.Common.common.SendMail("ShopOnLine", "Đơn hàng #" + idOrder, contentCustomer.ToString(), Email);
            // email addmin
            string contentAdmin = System.IO.File.ReadAllText(Server.MapPath("~/TemplateEmail/send1.html"));
            contentAdmin = contentAdmin.Replace("{{MaDH}}", idOrder.ToString());
            contentAdmin = contentAdmin.Replace("{{SanPham}}", strSanPham);
            contentAdmin = contentAdmin.Replace("{{NgayDatHang}}", DateTime.Now.ToString("dd-MM-yyyy"));
            contentAdmin = contentAdmin.Replace("{{TenKhachHang}}", TenKH);
            contentAdmin = contentAdmin.Replace("{{Phone}}", Mobile);
            contentAdmin = contentAdmin.Replace("{{Email}}", Email);
            contentAdmin = contentAdmin.Replace("{{TienGiam}}", tienGiam.ToString("N0"));
            if (type == 2)
            {
                contentAdmin = contentAdmin.Replace("{{Payments}}", "Chuyển khoản ngân hàng");
            }
            else if (type == 1)
            {
                contentAdmin = contentAdmin.Replace("{{Payments}}", "Thanh toán khi nhận hàng");
            }
            contentAdmin = contentAdmin.Replace("{{Address}}", Address);
            contentAdmin = contentAdmin.Replace("{{ThanhTien}}", thanhtien.ToString("N0"));
            contentAdmin = contentAdmin.Replace("{{TongTien}}", TongTien.ToString("N0"));
            Web_Sach.Common.common.SendMail("ShopOnLine", "Đơn hàng mới #" + idOrder, contentAdmin.ToString(), ConfigurationManager.AppSettings["EmailAdmin"]);

            //send mail
            // thanh toasn vnpay
            if (type == 2)
            {
                var url = UrlPayment(int.Parse(TypePaymentVN), order.ID);

                code = new { Success = true, Code = type, Url = url, DonGia = 0 };
            }
            else if (type == 1)
            {  // thanh toán khi nhận hàng
                Session["checkVoucher"] = null;// thiết lập lại voucher
                Session[SessionHelper.CART_KEY] = null;// đặt hàng giỏ hàng sẽ trống

            }
            return Json(code);
        }
        [HttpGet]
        //kích thanh toán chạy 
        public ActionResult Payment()
        {

            var cart = Session[SessionHelper.CART_KEY];
            var userPayment = (UserLoginSession)Session[SessionHelper.USER_KEY];
            ViewBag.user = userPayment;

            var list = new List<CartItem>();
            if (cart != null)
            {
                list = cart as List<CartItem>;
            }
            return View(list);
        }
        [HttpPost]
        public ActionResult AddVoucher(Order obOrder)
        {
            var code = new { Success = false, Code = 1, Url = "", DonGia = 0 };
            //var order = new DonHang();
            var checkVoucher = new checkVoucher();
            var checkVoucherInput = new checkVoucher();
            var sessionVoucher = Session["checkVoucher"];
            if (sessionVoucher == null)
            {// chưa tồn tại voucher
                checkVoucherInput.maVoucher = obOrder.mavoucher;

                checkVoucherInput.Status = 0;
                Session["checkVoucher"] = checkVoucherInput;
                checkVoucher = (checkVoucher)Session["checkVoucher"];
            }
            else
            {// tồn tại voucher
                checkVoucher = (checkVoucher)Session["checkVoucher"];
                checkVoucherInput.maVoucher = checkVoucher.maVoucher;
                checkVoucherInput.Status = 1;
                Session["checkVoucher"] = checkVoucherInput;
                checkVoucher = (checkVoucher)Session["checkVoucher"];
            }
            var dbVoucher = db.Vouchers.Where(x => x.MaVoucher == obOrder.mavoucher).FirstOrDefault();

            if (!string.IsNullOrEmpty(obOrder.mavoucher))
            {// đã điền voucher
             //ktra xem mã có tồn tại không

                if (dbVoucher != null)
                {// voucher tồn tại

                    if (checkVoucher.Status == 0)
                    {// kiểm tra ngày sử dụng

                        if (DateTime.Now >= dbVoucher.NgayTao && DateTime.Now <= dbVoucher.NgayHetHan)
                        {// trong thời gian  sử dụng
                         //kiểm tra đơn tối thiểu tm không
                            if (obOrder.total >= dbVoucher.DonGiaToiThieu)
                            {// thỏa mã đơn giá
                             // check số lần sử dụng
                                if (dbVoucher.SoLanDaSuDung <= dbVoucher.SoLanSuDung)
                                {// đc add voucher
                                 //order.TongTien = obOrder.total - (decimal)dbVoucher.SoTienGiam;
                                 //dbVoucher.SoLanDaSuDung = dbVoucher.SoLanDaSuDung + 1;
                                    int price = (int)dbVoucher.SoTienGiam;
                                    code = new { Success = true, Code = 14, Url = "", DonGia = price }; // chưa tồn tại
                                                                                                        //checkVoucher.maVoucher = dbVoucher.MaVoucher;
                                                                                                        //checkVoucher.Status = 1;
                                                                                                        //Session["checkVoucher"] = checkVoucher;
                                                                                                        //db.SaveChanges();
                                    return Json(code);
                                }
                                else
                                {// không được add voucher
                                    code = new { Success = false, Code = 13, Url = "", DonGia = 0 }; // chưa tồn tại
                                    return Json(code);
                                }
                            }
                            else
                            {// không thỏa mãn đơn giá
                                int price = (int)dbVoucher.DonGiaToiThieu;

                                code = new { Success = false, Code = 12, Url = "", DonGia = price }; // chưa tồn tại
                                return Json(code);
                            }
                        }
                        else
                        {// hết thời gian sử dụng
                            code = new { Success = false, Code = 11, Url = "", DonGia = 0 }; // chưa tồn tại
                            return Json(code);
                        }
                    }


                    if (checkVoucher.maVoucher != obOrder.mavoucher)
                    {// mã trước và sau khác nhau

                        // kiểm tra ngày sử dụng

                        if (DateTime.Now >= dbVoucher.NgayTao && DateTime.Now <= dbVoucher.NgayHetHan)
                        {// trong thời gian  sử dụng
                         //kiểm tra đơn tối thiểu tm không
                            if (obOrder.total >= dbVoucher.DonGiaToiThieu)
                            {// thỏa mã đơn giá
                             // check số lần sử dụng
                                if (dbVoucher.SoLanDaSuDung <= dbVoucher.SoLanSuDung)
                                {// đc add voucher
                                 //order.TongTien = obOrder.total - (decimal)dbVoucher.SoTienGiam;
                                 //dbVoucher.SoLanDaSuDung = dbVoucher.SoLanDaSuDung + 1;
                                    int price = (int)dbVoucher.SoTienGiam;
                                    code = new { Success = true, Code = 14, Url = "", DonGia = price }; // chưa tồn tại
                                    checkVoucher.maVoucher = dbVoucher.MaVoucher;

                                    Session["checkVoucher"] = checkVoucher;
                                    //db.SaveChanges();
                                    return Json(code);
                                }
                                else
                                {// không được add voucher
                                    code = new { Success = false, Code = 13, Url = "", DonGia = 0 }; // chưa tồn tại
                                    return Json(code);
                                }
                            }
                            else
                            {// không thỏa mãn đơn giá
                                int price = (int)dbVoucher.DonGiaToiThieu;

                                code = new { Success = false, Code = 12, Url = "", DonGia = price }; // chưa tồn tại
                                return Json(code);
                            }
                        }
                        else
                        {// hết thời gian sử dụng
                            code = new { Success = false, Code = 11, Url = "", DonGia = 0 }; // chưa tồn tại
                            return Json(code);
                        }


                    }
                    else
                    {// mã trước và sau giống nhau
                        int price = (int)dbVoucher.SoTienGiam;
                        code = new { Success = false, Code = 15, Url = "", DonGia = price }; // chưa tồn tại
                        return Json(code);
                    }

                }
                else
                {// voucher chưa tồn tại

                    code = new { Success = false, Code = 10, Url = "", DonGia = 0 }; // chưa tồn tại
                    return Json(code);
                }


            }
            return Json(code);
        }

        [HttpPost]
        public JsonResult PaymentPost(Order obOrder)
        {
            var code = new { Success = false, Code = 1, Url = "", DonGia = 0 };
            var order = new DonHang();
            var sessionUser = (UserLoginSession)Session[SessionHelper.USER_KEY];

            var tienGiam = decimal.Zero;
            if (!string.IsNullOrEmpty(obOrder.mavoucher))
            {// mã voucher 
                var dbVoucher = db.Vouchers.Where(x => x.MaVoucher == obOrder.mavoucher).FirstOrDefault();
                if (dbVoucher != null)
                {
                    order.TongTien = obOrder.total - (decimal)dbVoucher.SoTienGiam;
                    tienGiam = (decimal)dbVoucher.SoTienGiam;
                    order.MaVoucher = dbVoucher.ID;
                    dbVoucher.SoLanDaSuDung = dbVoucher.SoLanDaSuDung + 1;
                    db.Vouchers.Attach(dbVoucher);// theo dõi 
                    db.Entry(dbVoucher).Property(s => s.SoLanDaSuDung).IsModified = true;

                }
            }
            else
            {
                order.TongTien = obOrder.total;
                order.MaVoucher = null;
            }
            // Thiết lập thông tin đơn hàng
            order.TenNguoiNhan = obOrder.TenKH;
            order.Moblie = obOrder.Mobile;
            order.DiaChiNguoiNhan = obOrder.Address;
            order.Email = obOrder.Email;
            order.NgayDat = DateTime.Now;
            order.DaThanhToan = 0;
            order.MaKH = sessionUser.UserID;
            order.Status = 1;
            var cart = Session[SessionHelper.CART_KEY] as List<CartItem>;
            foreach (var item in cart)
            {
                // Tạo mới ChiTietDonHang
                order.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaSach = item.sach.ID,
                    Quantity = item.Quantity,
                    Price = (int)item.sach.Price,
                });

                // Cập nhật số lượng sách
                var sach = db.Saches.Find(item.sach.ID);

                sach.Quantity -= item.Quantity;
                db.Entry(sach).Property(s => s.Quantity).IsModified = true;
            }

            // Thêm đơn hàng vào cơ sở dữ liệu
            db.DonHangs.Add(order);
            db.SaveChanges();

            var idOrder = order.ID;
            int type = int.Parse(obOrder.TypePayment);
            code = new { Success = true, Code = type, Url = "", DonGia = 0 };
            //send mail
            var strSanPham = "";
            var thanhtien = decimal.Zero;
            var TongTien = decimal.Zero;
            foreach (var sp in cart)
            {
                strSanPham += "<tr>";
                strSanPham += "<td>" + sp.sach.Name + "</td>";
                strSanPham += "<td>" + sp.Quantity + "</td>";
                strSanPham += "<td>" + (sp.sach.Price.HasValue ? sp.sach.Price.Value.ToString("N0") : "") + "</td>";
                strSanPham += "</tr>";
            }

            thanhtien = obOrder.total;
            TongTien = thanhtien - tienGiam;

            string contentCustomer = System.IO.File.ReadAllText(Server.MapPath("~/TemplateEmail/send2.html"));
            contentCustomer = contentCustomer.Replace("{{MaDH}}", idOrder.ToString());
            contentCustomer = contentCustomer.Replace("{{SanPham}}", strSanPham);
            contentCustomer = contentCustomer.Replace("{{NgayDatHang}}", DateTime.Now.ToString("dd-MM-yyyy"));
            contentCustomer = contentCustomer.Replace("{{TenKhachHang}}", obOrder.TenKH);
            contentCustomer = contentCustomer.Replace("{{Phone}}", obOrder.Mobile);
            contentCustomer = contentCustomer.Replace("{{Email}}", obOrder.Email);
            if (type == 2)
            {
                contentCustomer = contentCustomer.Replace("{{Payments}}", "Chuyển khoản ngân hàng");
            }
            else if (type == 1)
            {
                contentCustomer = contentCustomer.Replace("{{Payments}}", "Thanh toán khi nhận hàng");
            }
            contentCustomer = contentCustomer.Replace("{{TienGiam}}", tienGiam.ToString("N0"));
            contentCustomer = contentCustomer.Replace("{{Address}}", obOrder.Address);
            contentCustomer = contentCustomer.Replace("{{ThanhTien}}", thanhtien.ToString("N0"));
            contentCustomer = contentCustomer.Replace("{{TongTien}}", TongTien.ToString("N0"));
            Web_Sach.Common.common.SendMail("ShopOnLine", "Đơn hàng #" + idOrder, contentCustomer.ToString(), obOrder.Email);
            // email addmin
            string contentAdmin = System.IO.File.ReadAllText(Server.MapPath("~/TemplateEmail/send1.html"));
            contentAdmin = contentAdmin.Replace("{{MaDH}}", idOrder.ToString());
            contentAdmin = contentAdmin.Replace("{{SanPham}}", strSanPham);
            contentAdmin = contentAdmin.Replace("{{NgayDatHang}}", DateTime.Now.ToString("dd-MM-yyyy"));
            contentAdmin = contentAdmin.Replace("{{TenKhachHang}}", obOrder.TenKH);
            contentAdmin = contentAdmin.Replace("{{Phone}}", obOrder.Mobile);
            contentAdmin = contentAdmin.Replace("{{Email}}", obOrder.Email);
            contentAdmin = contentAdmin.Replace("{{TienGiam}}", tienGiam.ToString("N0"));
            if (type == 2)
            {
                contentAdmin = contentAdmin.Replace("{{Payments}}", "Chuyển khoản ngân hàng");
            }
            else if (type == 1)
            {
                contentAdmin = contentAdmin.Replace("{{Payments}}", "Thanh toán khi nhận hàng");
            }
            contentAdmin = contentAdmin.Replace("{{Address}}", obOrder.Address);
            contentAdmin = contentAdmin.Replace("{{ThanhTien}}", thanhtien.ToString("N0"));
            contentAdmin = contentAdmin.Replace("{{TongTien}}", TongTien.ToString("N0"));
            Web_Sach.Common.common.SendMail("ShopOnLine", "Đơn hàng mới #" + idOrder, contentAdmin.ToString(), ConfigurationManager.AppSettings["EmailAdmin"]);

            //send mail

            // thanh toasn vnpay
            if (type == 2)
            {
                var url = UrlPayment(int.Parse(obOrder.TypePaymentVN), order.ID);

                code = new { Success = true, Code = type, Url = url, DonGia = 0 };
            }
            else if (type == 1)
            {  // thanh toán khi nhận hàng
                Session["checkVoucher"] = null;// thiết lập lại voucher
                Session[SessionHelper.CART_KEY] = null;// đặt hàng giỏ hàng sẽ trống

            }

            return Json(code);

        }

        [ChildActionOnly]
        public ActionResult Order()
        {
            var sessionUser = Session[SessionHelper.USER_KEY] as UserLoginSession;
            if (sessionUser != null)
            {
                var order = from dh in db.DonHangs
                            where dh.MaKH == sessionUser.UserID
                            select dh;

                //ViewBag.OrderAll = order.ToList();
                if (order != null)
                {
                    TempData["OrderPending"] = order.Where(x => x.Status == 1).ToList();
                    TempData["OrderPack"] = order.Where(x => x.Status == 2).ToList();
                    TempData["OrderTransport"] = order.Where(x => x.Status == 3).ToList();
                    TempData["OrderSuccess"] = order.Where(x => x.Status == 4).ToList();
                    TempData["OrderFailure"] = order.Where(x => x.Status == 5).ToList();
                }

            }
            return PartialView();
        }

        // xem chi tiết đơn hàng
        public ActionResult OrderDetail(int Id)
        {
            var orderDetail = from dh in db.DonHangs
                              join dt in db.ChiTietDonHangs on dh.ID equals dt.MaDonHang
                              join sach in db.Saches on dt.MaSach equals sach.ID
                              where dh.ID == Id
                              select new OrderDetail()
                              {
                                  sachId = sach.ID,
                                  Image = sach.Images.FirstOrDefault(x => x.MaSP == sach.ID && x.IsDefault).Image1,
                                  sachName = sach.Name,
                                  metaTile = sach.MetaTitle,
                                  PriceBuy = (double)dt.Price,
                                  QuantityBuy = (int)dt.Quantity,
                                  Status = dh.DaThanhToan,

                              };
            var voucherOrder = from dh in db.DonHangs
                               join v in db.Vouchers on dh.MaVoucher equals v.ID
                               where dh.ID == Id
                               select new VoucherOrder()
                               {
                                   maVoucher = dh.MaVoucher,
                                   SoTienGiams = v.SoTienGiam
                               };
            ViewBag.voucherOrder = voucherOrder.FirstOrDefault();
            ViewBag.OrderDetail = orderDetail.ToList();
            ViewBag.OrderId = Id;

            ViewBag.DaThanhToan = orderDetail.Select(x => x.Status).FirstOrDefault();
            return View();
        }
        // thanh toán vnpay
        public string UrlPayment(int TypePaymentVN, int orderCode)
        {
            var urlPayment = "";
            var order = db.DonHangs.FirstOrDefault(x => x.ID == orderCode);
            //Get Config Info
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhan ket qua tra ve 
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Secret Key

            //Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();
            var Price = (long)order.TongTien * 100;
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", Price.ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            if (TypePaymentVN == 1)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNPAYQR");
            }
            else if (TypePaymentVN == 2)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNBANK");
            }
            else if (TypePaymentVN == 3)
            {
                vnpay.AddRequestData("vnp_BankCode", "INTCARD");
            }
            //order.NgayDat.Value.ToString("yyyyMMddHHmmss")
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);
            vnpay.AddRequestData("vnp_CreateDate", cstTime.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");

            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + order.ID);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.ID.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày


            urlPayment = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            //log.InfoFormat("VNPAY URL: {0}", paymentUrl);
            //Response.Redirect(paymentUrl);
            return urlPayment;

        }

        // đang ký thành công nhảy sang đây
        public ActionResult VnpayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuoi bi mat
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    //get all querystring data
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }
                //vnp_TxnRef: Ma don hang merchant gui VNPAY tai command=pay    
                //vnp_TransactionNo: Ma GD tai he thong VNPAY
                //vnp_ResponseCode:Response code from VNPAY: 00: Thanh cong, Khac 00: Xem tai lieu
                //vnp_SecureHash: HmacSHA512 cua du lieu tra ve

                long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                String vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                String TerminalID = Request.QueryString["vnp_TmnCode"];
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
                String bankCode = Request.QueryString["vnp_BankCode"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        var itemOrder = db.DonHangs.FirstOrDefault(x => x.ID == orderId);
                        if (itemOrder != null)
                        {
                            /* Session["checkVoucher"] = null;*/// thiết lập lại voucher
                            Session[SessionHelper.CART_KEY] = null;// đặt hàng giỏ hàng sẽ trống
                            itemOrder.DaThanhToan = 1;// đã thanh toán

                            db.DonHangs.Attach(itemOrder);// theo dõi 
                            db.Entry(itemOrder).Property(s => s.DaThanhToan).IsModified = true;

                            //  db.Entry(itemOrder).State = System.Data.Entity.EntityState.Modified;// cập nhật lại trong csdl
                            db.SaveChanges();
                        }
                        //Thanh toan thanh cong
                        ViewBag.ThanhToanThanhCong = "Số tiền thanh toán (VND):" + vnp_Amount.ToString("N0");
                        ViewBag.InnerText = "Giao dịch được thực hiện thành công. Cảm ơn quý khách đã sử dụng dịch vụ";
                        //log.InfoFormat("Thanh toan thanh cong, OrderId={0}, VNPAY TranId={1}", orderId, vnpayTranId);
                    }
                    else
                    {
                        /* Session["checkVoucher"] = null;*/// thiết lập lại voucher
                        Session[SessionHelper.CART_KEY] = null;// đặt hàng giỏ hàng sẽ trống
                        var order = db.DonHangs.FirstOrDefault(x => x.ID == orderId);
                        if (order != null)
                        {
                            var orderDetail = db.ChiTietDonHangs.Where(x => x.MaDonHang == orderId);
                            foreach (var item in orderDetail)
                            {// cập nhật số lượng
                                var sachUpdate = db.Saches.Find(item.MaSach);// cập nhật lại số lượng sản phẩm
                                sachUpdate.Quantity = sachUpdate.Quantity + item.Quantity;


                            }
                            db.ChiTietDonHangs.RemoveRange(orderDetail);
                            db.DonHangs.Remove(order);

                            db.SaveChanges();
                        }
                        //Thanh toan khong thanh cong. Ma loi: vnp_ResponseCode
                        ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý.Mã lỗi: " + vnp_ResponseCode;
                    }

                }

            }
            return View();
        }



    }
}