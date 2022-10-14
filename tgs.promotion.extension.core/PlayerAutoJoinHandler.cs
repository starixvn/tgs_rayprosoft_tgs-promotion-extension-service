using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Models.Promotion;
using tgs.promotion.extension.core.Models;
using Tgs.VipLevelService.Clients;

namespace tgs.promotion.extension.core;

public class PlayerAutoJoinHandler : IRequestHandler<PlayerAutoJoinRequest, Unit>
{
    private readonly ILogger<PlayerAutoJoinHandler> _logger;
    private readonly IPlayerServiceClient _playerServiceClient;
    private readonly IPromotionClient _promotionClient;
    private readonly IPromotionServiceClient _promotionServiceClient;
    private readonly IVipLevelServiceClient _vipLevelServiceClient;

    public PlayerAutoJoinHandler(
        IPlayerServiceClient playerServiceClient,
        IPromotionClient promotionClient,
        IPromotionServiceClient promotionServiceClient,
        IVipLevelServiceClient vipLevelServiceClient,
        ILogger<PlayerAutoJoinHandler> logger)
    {
        _playerServiceClient = playerServiceClient;
        _promotionClient = promotionClient;
        _promotionServiceClient = promotionServiceClient;
        _vipLevelServiceClient = vipLevelServiceClient;
        _logger = logger;
    }

    public async Task<Unit> Handle(PlayerAutoJoinRequest request, CancellationToken cancellationToken)
    {
        var getPromotionRequest = new GetPromotionsRequest
        {
            Status = (short)PromotionStatus.Enabled,
            ShowAll = true
        };

        var promotions = await _promotionClient.GetPromotionsAsync(getPromotionRequest)
            .ConfigureAwait(false);

        var brandIds = promotions.Items.Select(x => x.BrandId).Distinct();

        foreach (var brandId in brandIds)
        {
            var brandAllVipLevels = await _vipLevelServiceClient.GetBrandAllVipLevelsAsync(brandId, cancellationToken)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var promotion in promotions.Items.Where(x => x.BrandId == brandId))
            {
                var promotionVipLevelIds = brandAllVipLevels.Where(x => promotion.VipLevels.Contains(x.Level))
                    .Select(x => x.Id).ToArray();

                var promotionRule =
                    await _promotionServiceClient.GetPromotionRuleAsync<PromotionRuleInfo>(promotion.Id,
                        cancellationToken)
                        .ConfigureAwait(false);

                if (promotionRule is null)
                {
                    _logger.LogWarning($"promotion rule not found. ({promotion.Id}:{promotion.Name})");
                    continue;
                }

                var settings = promotionRule.CampaignDetail.Deserialize<PlayerAutoJoinModel>(new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true });

                if (settings is null)
                {
                    _logger.LogInformation($"promotion campaign detail not found. ({promotion.Id}:{promotion.Name})");
                    continue;
                }

                if (settings.PlayerAutoJoin)
                {
                    var query = new PlayerListQuery
                    {
                        Status = PlayerStatus.Enabled
                    };

                    var players = _playerServiceClient.QueryPlayerListAsync(brandId, query, cancellationToken)
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false);

                    await foreach (var player in players)
                        if (promotionVipLevelIds.Contains(player.VipId!.Value))
                        {
                            var promotionRequest = new JoinPromotionRequest(promotion.Id, player.PlayerCode);

                            _ = await _promotionClient.JoinAsync(promotionRequest)
                                .ConfigureAwait(false);
                        }
                }
            }
        }

        return Unit.Value;
    }
}