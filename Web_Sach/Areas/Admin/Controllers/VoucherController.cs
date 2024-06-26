﻿using OfficeOpenXml.Style;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Web_Sach.Models;
using Web_Sach.Models.EF;

namespace Web_Sach.Areas.Admin.Controllers
{
    public class VoucherController : BaseController
    {
        // GET: Admin/ProductCategory
        public ActionResult Index(string searchString, int page = 1, int pageSize = 20)
        {
            var voucher = new VoucherModels().listPage(searchString, page, pageSize);
            ViewBag.SearchString = searchString;

            return View(voucher);
        }

        [HttpGet]

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]

        public ActionResult Create(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                var voucherCheck = new VoucherModels().uniqueName(voucher.MaVoucher);
                if (voucherCheck != null)
                {
                    ModelState.AddModelError("MaVoucher", "Mã voucher đã tồn tại");
                    return View(voucher);
                }
                if (voucher.SoLanSuDung < 0)
                {
                    ModelState.AddModelError("SoLanSuDung", "Số lần sử dụng phải lớn hơn 0");
                    return View(voucher);
                }
                else if (voucher.DonGiaToiThieu < 0)
                {
                    ModelState.AddModelError("DonGiaToiThieu", "Đơn giá tối thiểu phải lớn hơn 0");
                    return View(voucher);
                }
                else if (voucher.SoTienGiam < 0)
                {
                    ModelState.AddModelError("SoTienGiam", "Số tiền giảm phải lớn hơn 0");
                    return View(voucher);
                }
                else if (voucher.NgayTao > voucher.NgayHetHan)
                {
                    ModelState.AddModelError("NgayHetHan", "Ngày hết hạn phải lớn hơn ngày tạo!");

                    return View(voucher);
                }

                var voucherIndex = new VoucherModels().Insert(voucher);
                if (voucherIndex > 0)
                {
                    SetAlert("Thêm voucher thành công", "success");
                    return RedirectToAction("Index");
                }
            }
            else
            {
                SetAlert("Thêm voucher thất bại", "error");
            }

            return View(voucher);
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            var voucherItem = new VoucherModels().Edit(id);
            return View(voucherItem);
        }

        [HttpPost]
        public ActionResult Update(Voucher dm)
        {
            if (ModelState.IsValid)
            {
                var voucher = new VoucherModels();

                if (voucher.Compare(dm))
                {
                    ModelState.AddModelError("MaVoucher", "Mã voucher đã tồn tại");
                    return View(dm);
                }
                if (dm.SoLanSuDung < 0)
                {
                    ModelState.AddModelError("SoLanSuDung", "Số lần sử dụng phải lớn hơn 0");
                    return View(dm);
                }
                else if (dm.DonGiaToiThieu < 0)
                {
                    ModelState.AddModelError("DonGiaToiThieu", "Đơn giá tối thiểu phải lớn hơn 0");
                    return View(dm);
                }
                else if (dm.SoTienGiam < 0)
                {
                    ModelState.AddModelError("SoTienGiam", "Số tiền giảm phải lớn hơn 0");
                    return View(dm);
                }
                else if (dm.NgayTao > dm.NgayHetHan)
                {
                    ModelState.AddModelError("NgayHetHan", "Ngày hết hạn phải lớn hơn ngày tạo!");

                    return View(dm);
                }

                if (voucher.Update(dm))
                {
                    SetAlert("Cập nhật thành công", "success");
                    return RedirectToAction("Index", "Voucher");

                }
                else
                {
                    SetAlert("Cập nhật thất bại", "error");

                }



            }
            return View(dm);
        }


        [HttpDelete]
        public ActionResult Delete(int id)
        {
            new VoucherModels().Delete(id);
            return RedirectToAction("Index", "Voucher");
        }

        public void ExportExcel_EPPLUS()
        {
            using (WebSachDb db = new WebSachDb())
            {
                var list = db.Vouchers.ToList();
                int Out_TotalRecord = list.Count();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                ExcelPackage Ep = new ExcelPackage();

                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");
                Sheet.Cells["A1"].Value = "ID";
                Sheet.Cells["A1"].Style.Font.Bold = true;
                Sheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                Sheet.Cells["B1"].Value = "Đơn giá tối thiểu";
                Sheet.Cells["B1"].Style.Font.Bold = true;
                Sheet.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                Sheet.Cells["C1"].Value = "Ngày tạo";
                Sheet.Cells["C1"].Style.Font.Bold = true;
                Sheet.Cells["C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                Sheet.Cells["D1"].Value = "Ngày hết hạn";
                Sheet.Cells["D1"].Style.Font.Bold = true;
                Sheet.Cells["D1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                Sheet.Cells["E1"].Value = "Mã voucher";
                Sheet.Cells["E1"].Style.Font.Bold = true;
                Sheet.Cells["E1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                Sheet.Cells["F1"].Value = "Số tiền giảm";
                Sheet.Cells["F1"].Style.Font.Bold = true;
                Sheet.Cells["F1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                Sheet.Cells["G1"].Value = "Số lần sử dụng";
                Sheet.Cells["G1"].Style.Font.Bold = true;
                Sheet.Cells["G1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                Sheet.Cells["H1"].Value = "Số lần đã sử dụng";
                Sheet.Cells["H1"].Style.Font.Bold = true;
                Sheet.Cells["H1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                int row = 2;
                if (Out_TotalRecord > 0)
                {
                    foreach (var item in list)
                    {
                        Sheet.Cells[string.Format("A{0}", row)].Value = item.ID;
                        Sheet.Cells[string.Format("A{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        Sheet.Cells[string.Format("B{0}", row)].Value = item.DonGiaToiThieu;
                        Sheet.Cells[string.Format("B{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        Sheet.Cells[string.Format("C{0}", row)].Value = item.NgayTao?.ToString("dd/MM/yyyy");
                        Sheet.Cells[string.Format("C{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        Sheet.Cells[string.Format("D{0}", row)].Value = item.NgayHetHan?.ToString("dd/MM/yyyy");
                        Sheet.Cells[string.Format("D{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        Sheet.Cells[string.Format("E{0}", row)].Value = item.MaVoucher;
                        Sheet.Cells[string.Format("E{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        Sheet.Cells[string.Format("F{0}", row)].Value = item.SoTienGiam;
                        Sheet.Cells[string.Format("F{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        Sheet.Cells[string.Format("G{0}", row)].Value = item.SoLanSuDung;
                        Sheet.Cells[string.Format("G{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        Sheet.Cells[string.Format("H{0}", row)].Value = item.SoLanDaSuDung;
                        Sheet.Cells[string.Format("H{0}", row)].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                     
                        row++;
                    }
                }
                Sheet.Cells["A:AZ"].AutoFitColumns();
                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment: filename=" + "Report.xlsx");
                Response.BinaryWrite(Ep.GetAsByteArray());
                Response.End();
            }

        }


    }
}