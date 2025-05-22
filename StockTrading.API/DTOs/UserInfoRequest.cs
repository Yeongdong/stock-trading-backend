using System.ComponentModel.DataAnnotations;

namespace stock_trading_backend.DTOs;

public class UserInfoRequest
{
    [Required(ErrorMessage = "AppKey는 필수 항목입니다.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "AppKey는 1-100자 사이여야 합니다.")]
    public string AppKey { get; set; }

    [Required(ErrorMessage = "AppSecret은 필수 항목입니다.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "AppSecret은 1-100자 사이여야 합니다.")]
    public string AppSecret { get; set; }

    [Required(ErrorMessage = "계좌번호는 필수 항목입니다.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "계좌번호는 10-20자 사이여야 합니다.")]
    public string AccountNumber { get; set; }
}