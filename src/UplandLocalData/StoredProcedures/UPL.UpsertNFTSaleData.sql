CREATE PROCEDURE [UPL].[UpsertNFTSaleData]
(
	@Id              INT,
	@DGoodId         INT,
	@SellerEOS       VARCHAR(12),
	@BuyerEOS        VARCHAR(12),
	@Amount          DECIMAL(11,2),
	@AmountFiat      DECIMAL(11,2),
	@DateTime        DATETIME,
	@SubMerchant     VARCHAR(12),
	@SubMerchantFee  DECIMAL(11,2)
)
AS
BEGIN
	BEGIN TRY	
		IF(@Id < 0)
			BEGIN
				INSERT INTO [UPL].[NFTSaleData]
				(
					[DGoodId],
					[SellerEOS],
					[BuyerEOS],
					[Amount],
					[AmountFiat],
					[DateTime],
					[SubMerchant],
					[SubMerchantFee]
				)
				Values
				(
					@DGoodId,
					@SellerEOS,
					@BuyerEOS,
					@Amount,
					@AmountFiat,
					@DateTime,
					@SubMerchant,
					@SubMerchantFee
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[NFTSaleData]
				SET
					[DGoodId] = @DGoodId,
					[SellerEOS] = @SellerEOS,
					[BuyerEOS] = @BuyerEOS,
					[Amount] = @Amount,
					[AmountFiat] = @AmountFiat,
					[DateTime] = @DateTime,
					[SubMerchant] = @SubMerchant,
					[SubMerchantFee] = @SubMerchantFee
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