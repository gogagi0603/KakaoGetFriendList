using Kakao.Models;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Kakao.Services;

public class ExcelExportService
{
    private static readonly Color KakaoYellow = Color.FromArgb(0xFE, 0xE5, 0x00);
    private static readonly Color KakaoBrown = Color.FromArgb(0x3C, 0x1E, 0x1E);
    private static readonly Color AutoFillColor = Color.FromArgb(0xF0, 0xF0, 0xF0);

    public byte[] ExportFriendsToExcel(List<KakaoFriend> friends)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("청첩장 발송 목록");

        WriteHeader(sheet);
        WriteData(sheet, friends);
        AddDropdownValidations(sheet, friends.Count);
        StyleSheet(sheet, friends.Count);

        return package.GetAsByteArray();
    }

    public byte[] ExportPickerFriendsToExcel(List<PickerFriend> friends)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("청첩장 발송 목록");

        WriteHeader(sheet);
        WritePickerData(sheet, friends);
        AddDropdownValidations(sheet, friends.Count);
        StyleSheet(sheet, friends.Count);

        return package.GetAsByteArray();
    }

    private static void WriteHeader(ExcelWorksheet sheet)
    {
        var headers = new[]
        {
            "번호", "카카오닉네임", "이름", "구분", "관계",
            "실물청첩장", "모바일청첩장", "주소", "연락처", "즐겨찾기", "메모"
        };

        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = sheet.Cells[1, col];
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(KakaoBrown);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(KakaoYellow);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            cell.Style.Border.Bottom.Color.SetColor(Color.FromArgb(0xCC, 0xB8, 0x00));
        }
    }

    private static void WriteData(ExcelWorksheet sheet, List<KakaoFriend> friends)
    {
        for (int i = 0; i < friends.Count; i++)
        {
            int row = i + 2;
            var f = friends[i];

            // 카카오 API에서 자동 채워지는 열 (회색 배경)
            SetAutoCell(sheet.Cells[row, 1], i + 1);           // 번호
            SetAutoCell(sheet.Cells[row, 2], f.ProfileNickname); // 카카오닉네임
            SetAutoCell(sheet.Cells[row, 10], f.Favorite ? "O" : ""); // 즐겨찾기

            // 수동 입력 열: 이름(3), 구분(4), 관계(5), 실물(6), 모바일(7), 주소(8), 연락처(9), 메모(11)
            // 빈칸으로 두고 테두리만 적용

            // 행 배경: 짝수 행 연한 색
            if (i % 2 == 1)
            {
                for (int col = 3; col <= 11; col++)
                {
                    if (col == 10) continue;
                    sheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0xFA, 0xFA, 0xFA));
                }
            }
        }
    }

    private static void WritePickerData(ExcelWorksheet sheet, List<PickerFriend> friends)
    {
        for (int i = 0; i < friends.Count; i++)
        {
            int row = i + 2;
            var f = friends[i];

            SetAutoCell(sheet.Cells[row, 1], i + 1);
            SetAutoCell(sheet.Cells[row, 2], f.ProfileNickname);
            SetAutoCell(sheet.Cells[row, 10], "");

            if (i % 2 == 1)
            {
                for (int col = 3; col <= 11; col++)
                {
                    if (col == 10) continue;
                    sheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0xFA, 0xFA, 0xFA));
                }
            }
        }
    }

    private static void SetAutoCell(ExcelRange cell, object value)
    {
        cell.Value = value;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(AutoFillColor);
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    }

    private static void AddDropdownValidations(ExcelWorksheet sheet, int count)
    {
        if (count == 0) return;

        string dataRange = $"2:{count + 1}";

        // 구분: 신랑측 / 신부측
        var vDiv = sheet.DataValidations.AddListValidation($"D{dataRange}");
        vDiv.ShowErrorMessage = false;
        vDiv.Formula.Values.Add("신랑측");
        vDiv.Formula.Values.Add("신부측");

        // 관계: 가족 / 친구 / 직장 / 기타
        var vRel = sheet.DataValidations.AddListValidation($"E{dataRange}");
        vRel.ShowErrorMessage = false;
        vRel.Formula.Values.Add("가족");
        vRel.Formula.Values.Add("친구");
        vRel.Formula.Values.Add("직장");
        vRel.Formula.Values.Add("기타");

        // 실물청첩장
        var vPhy = sheet.DataValidations.AddListValidation($"F{dataRange}");
        vPhy.ShowErrorMessage = false;
        vPhy.Formula.Values.Add("O");
        vPhy.Formula.Values.Add("X");
        vPhy.Formula.Values.Add("미정");

        // 모바일청첩장
        var vMob = sheet.DataValidations.AddListValidation($"G{dataRange}");
        vMob.ShowErrorMessage = false;
        vMob.Formula.Values.Add("O");
        vMob.Formula.Values.Add("X");
        vMob.Formula.Values.Add("미정");
    }

    private static void StyleSheet(ExcelWorksheet sheet, int count)
    {
        int lastRow = count + 1;

        // 전체 데이터 범위 테두리
        if (count > 0)
        {
            var dataRange = sheet.Cells[1, 1, lastRow, 11];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        // 열 너비 설정
        sheet.Column(1).Width = 6;   // 번호
        sheet.Column(2).Width = 20;  // 카카오닉네임
        sheet.Column(3).Width = 14;  // 이름
        sheet.Column(4).Width = 10;  // 구분
        sheet.Column(5).Width = 10;  // 관계
        sheet.Column(6).Width = 12;  // 실물청첩장
        sheet.Column(7).Width = 14;  // 모바일청첩장
        sheet.Column(8).Width = 30;  // 주소
        sheet.Column(9).Width = 14;  // 연락처
        sheet.Column(10).Width = 10; // 즐겨찾기
        sheet.Column(11).Width = 20; // 메모

        // 행 높이
        sheet.Row(1).Height = 22;
        for (int r = 2; r <= lastRow; r++)
            sheet.Row(r).Height = 20;

        // 헤더 행 고정
        sheet.View.FreezePanes(2, 1);
    }
}
