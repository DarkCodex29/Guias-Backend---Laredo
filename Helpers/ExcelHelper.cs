using NPOI.SS.UserModel;

namespace GuiasBackend.Helpers
{
    public static class ExcelHelper
    {
        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRowEmpty(IRow row)
        {
            if (row == null) return true;
            
            for (int i = row.FirstCellNum; i < row.LastCellNum; i++)
            {
                var cell = row.GetCell(i);
                if (cell != null && cell.CellType != CellType.Blank && !string.IsNullOrWhiteSpace(cell.ToString()))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public static string GetCellValueAsString(ICell? cell)
        {
            if (cell == null)
                return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CellFormula,
                _ => string.Empty
            };
        }
    }
}
