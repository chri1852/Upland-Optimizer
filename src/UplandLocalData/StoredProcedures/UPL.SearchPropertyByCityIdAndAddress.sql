CREATE PROCEDURE [UPL].[SearchPropertyByCityIdAndAddress]
(
	@CityId INT,
	@Address VARCHAR(200)
)
AS
BEGIN
	BEGIN TRY
		IF @CityId = 0
			BEGIN
				SELECT P.Id, P.CityId, P.Address, P.StreetId, P.NeighborhoodId, P.Size, P.Mint, P.Status, P.FSA, U.UplandUsername AS 'Owner', S.StructureType AS 'Building'
				FROM UPL.Property P (NOLOCK)
					LEFT JOIN UPL.EOSUser U (NOLOCK)
						ON P.Owner = U.EOSAccount
					LEFT JOIN UPL.PropertyStructure S (NOLOCK)
						ON P.Id = S.PropertyId
				WHERE P.Address Like '%' + @Address + '%'
			END
		ELSE
			BEGIN
				SELECT P.Id, P.CityId, P.Address, P.StreetId, P.NeighborhoodId, P.Size, P.Mint, P.Status, P.FSA, U.UplandUsername AS 'Owner', S.StructureType AS 'Building'
				FROM UPL.Property P (NOLOCK)
					LEFT JOIN UPL.EOSUser U (NOLOCK)
						ON P.Owner = U.EOSAccount
					LEFT JOIN UPL.PropertyStructure S (NOLOCK)
						ON P.Id = S.PropertyId
				WHERE P.CityId = @CityId
					AND P.Address Like '%' + @Address + '%'
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
