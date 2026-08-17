using DragonCommonApplication.Repositories;
using DragonPlacementApi.Endpoints;
using DragonPlacementApi.Poco;
using DragonAssignment.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Shouldly;

namespace DragonUnitTests.DragonTests;

public class DragonDeleteTests
{
    [Fact]
    public async Task DeleteDragon_DragonExists_ExpectOkAndSavesOnce()
    {
        const int DRAGON_ID = 42;
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.Delete(DRAGON_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await DragonEndpoints.DeleteDragonAsync(unitOfWorkMock.Object, DRAGON_ID);

        //Assert
        response.Result.ShouldBeOfType<Ok<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteDragon_DragonNotFound_ExpectNotFoundAndDoesNotSave()
    {
        const int DRAGON_ID = 999;
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonRepository.Delete(DRAGON_ID)).Returns(DeleteResult.NotFound);

        //Act
        var response = await DragonEndpoints.DeleteDragonAsync(unitOfWorkMock.Object, DRAGON_ID);

        //Assert
        response.Result.ShouldBeOfType<NotFound<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteDragon_DragonHasAssignment_ExpectConflictAndDoesNotSave()
    {
        const int DRAGON_ID = 7;
        var unitOfWorkMock = new Mock<IDragonPlacementUnitOfWork>();
        unitOfWorkMock.Setup(u => u.DragonHasAnAssignment(DRAGON_ID)).ReturnsAsync(true);
        unitOfWorkMock.Setup(u => u.DragonRepository.Delete(DRAGON_ID)).Returns(DeleteResult.Deleted);

        //Act
        var response = await DragonEndpoints.DeleteDragonAsync(unitOfWorkMock.Object, DRAGON_ID);

        //Assert
        response.Result.ShouldBeOfType<Conflict<ValidatedResponse>>();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }
}
