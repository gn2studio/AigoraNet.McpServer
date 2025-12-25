using AigoraNet.Common.Entities;
using System.ComponentModel.DataAnnotations;

namespace AigoraNet.Common.DTO;

public class MemberDTO
{
    public int rowNum { get; set; } = 0;

    public string Id { get; set; } = string.Empty;

    public Guid GetId()
    {
        if (Guid.TryParse(this.Id, out Guid cid))
        {
            return cid;
        }
        else
        {
            return Guid.Empty;
        }
    }

    public string Email { get; set; } = string.Empty;

    public bool IsEmailConfirm { get; set; } = false;

    public DateTime? EmailConfirmDate { get; set; }

    [Required]
    public Member.MemberType Type { get; set; } = Member.MemberType.User;

    public string UserType
    {
        get
        {
            switch (this.Type)
            {
                case Member.MemberType.Admin:
                    return "관리자";
                case Member.MemberType.User:
                    return "회원";
                default:
                    return "미정의";
            }
        }
    }

    public string? Bio { get; set; }

    public string? NickName { get; set; }

    public string? Photo { get; set; }

    public DateTime RegistDate { get; set; }

    public MemberDTO()
    {
    }

    public MemberDTO(Member member)
    {
        this.Id = member.Id.ToString();
        this.Email = member.Email;
        this.NickName = member.NickName;
        this.Photo = member.Photo;
        this.Type = member.Type;
        this.EmailConfirmDate = member.EmailConfirmDate;
        this.IsEmailConfirm = member.IsEmailConfirm;
        this.Bio = member.Bio;
        this.RegistDate = member.Condition.RegistDate;
    }
}
