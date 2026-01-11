using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Members;

public record GetMemberQuery(string Id) : IBridgeRequest<ReturnValues<Member>>;
public record GetMembersQuery(SearchParameter paramData) : IBridgeRequest<ReturnValues<List<Member>>>;

public class GetMemberQueryHandler : IBridgeHandler<GetMemberQuery, ReturnValues<Member>>
{
    private readonly ILogger<GetMemberQueryHandler> _logger;
    private readonly DefaultContext _context;

    public GetMemberQueryHandler(ILogger<GetMemberQueryHandler> logger, DefaultContext _db) : base()
    {
        _logger = logger;
        _context = _db;
    }

    public async Task<ReturnValues<Member>> HandleAsync(GetMemberQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<Member>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var member = await _context.Members.AsNoTracking()
            .Where(x => x.Id == request.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (member is null)
        {
            result.SetError("Member not found");
            return result;
        }

        result.SetSuccess(1, member);

        return result;
    }
}

public class GetMembersQueryHandler : IBridgeHandler<GetMembersQuery, ReturnValues<List<Member>>>
{
    private readonly ILogger<GetMembersQueryHandler> _logger;
    private readonly DefaultContext _context;

    public GetMembersQueryHandler(ILogger<GetMembersQueryHandler> logger, DefaultContext _db) : base()
    {
        _logger = logger;
        _context = _db;
    }

    public async Task<ReturnValues<List<Member>>> HandleAsync(GetMembersQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<Member>>();

        List<Member> list = new List<Member>();

        var q = _context.Members.AsNoTracking().Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        list = await q.ToListAsync(ct);

        result.SetSuccess(1, list);

        return result;
    }
}

