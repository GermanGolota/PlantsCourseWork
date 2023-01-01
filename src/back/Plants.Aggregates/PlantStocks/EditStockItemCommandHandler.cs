﻿namespace Plants.Aggregates.PlantStocks;

internal class EditStockItemCommandHandler : ICommandHandler<EditStockItemCommand>
{
    private readonly IRepository<PlantStock> _stockRepository;
    private readonly FileUploader _uploader;

    public EditStockItemCommandHandler(IRepository<PlantStock> stockRepository, FileUploader uploader)
    {
        _stockRepository = stockRepository;
        _uploader = uploader;
    }

    private PlantStock _stock;

    public async Task<CommandForbidden?> ShouldForbidAsync(EditStockItemCommand command, IUserIdentity user)
    {
        _stock ??= await _stockRepository.GetByIdAsync(command.Metadata.Aggregate.Id);
        var validIdentity = user.HasRole(Manager)
            .Or(user.HasRole(Producer).And(IsCaretaker(user, _stock)));
        //TODO: Should validate data here
        return validIdentity;
    }

    private CommandForbidden? IsCaretaker(IUserIdentity user, PlantStock plant) =>
        (user.UserName == plant.CaretakerUsername).ToForbidden("Cannot eddit somebody elses stock item");

    public async Task<IEnumerable<Event>> HandleAsync(EditStockItemCommand command)
    {
        _stock ??= await _stockRepository.GetByIdAsync(command.Metadata.Aggregate.Id);
        var newUrls = await _uploader.UploadAsync(_stock.Id, command.NewPictures);
        return new[]
        {
            new StockEdditedEvent(EventFactory.Shared.Create<StockEdditedEvent>(command), command.Plant, newUrls, command.RemovedPictureUrls)
        };
    }

}
