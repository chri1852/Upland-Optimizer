CREATE PROCEDURE [UPL].[UpsertSaleHistory]
(
	@Id              INT = NULL,
	@DateTime        DATETIME,
	@SellerEOS       VARCHAR(12) = NULL,
	@BuyerEOS        VARCHAR(12) = NULL,
	@PropId          BIGINT,
	@Amount          DECIMAL(20,2) = NULL,
	@OfferPropId     BIGINT = NULL,
	@Offer           BIT = FALSE,
	@Accepted        BIT = FALSE
)
AS
BEGIN
	BEGIN TRY	
		IF(@Id IS NULL)
			BEGIN
				INSERT INTO [UPL].[SaleHistory]
				(
					[DateTime],
					[SellerEOS],
					[BuyerEOS],
					[PropId],
					[Amount],
					[OfferPropId],
					[Offer],
					[Accepted]
				)
				Values
				(
					@DateTime,
					@SellerEOS,
					@BuyerEOS,
					@PropId,
					@Amount,
					@OfferPropId,
					@Offer,
					@Accepted
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[SaleHistory]
				SET
					[DateTime] = @DateTime,
					[SellerEOS] = @SellerEOS,
					[BuyerEOS] = @BuyerEOS,
					[PropId] = @PropId,
					[Amount] = @Amount,
					[OfferPropId] = @OfferPropId,
					[Offer] = @Offer,
					[Accepted] = @Accepted
				WHERE [Id] = @Id
			END
	END TRY

	BEGIN CATCH
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT @ErrorMessage = ERROR_MESSAGE(),
			   @ErrorSeverity = ERROR_SEVERITY(),
			   @ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
	END CATCH
END