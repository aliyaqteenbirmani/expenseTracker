DROP PROCEDURE IF EXISTS SP_AddSpendwise;
DROP PROCEDURE IF EXISTS SP_UpdateSpendwise;
DROP PROCEDURE IF EXISTS SP_GetAllSpendwises;
DROP PROCEDURE IF EXISTS SP_GetSpendwiseById;
DROP PROCEDURE IF EXISTS SP_DeleteSpendwise;

DELIMITER $$

CREATE PROCEDURE SP_AddSpendwise(
    IN p_Id CHAR(36),
    IN p_Name VARCHAR(255),
    IN p_CreatedOn DATETIME,
    IN p_CreatedBy NVARCHAR(200),
    IN p_ModifiedOn DATETIME,
    IN p_ModifiedBy NVARCHAR(30)
)
BEGIN
    INSERT INTO Spendwises
    (
        Id,
        Name,
        CreatedOn,
        CreatedBy,
        ModifiedOn,
        ModifiedBy,
        IsActive,
        IsDeleted
    )
    VALUES
    (
        p_Id,
        p_Name,
        p_CreatedOn,
        p_CreatedBy,
        p_ModifiedOn,
        p_ModifiedBy,
        1,
        0
    );

    SELECT *
    FROM Spendwises
    WHERE Id = p_Id;
END $$

CREATE PROCEDURE SP_UpdateSpendwise(
    IN p_Id CHAR(36),
    IN p_Name VARCHAR(255),
    IN p_ModifiedOn DATETIME,
    IN p_ModifiedBy NVARCHAR(30),
    IN p_IsActive BIT
)
BEGIN
    UPDATE Spendwises
    SET
        Name = p_Name,
        ModifiedOn = p_ModifiedOn,
        ModifiedBy = p_ModifiedBy,
        IsActive = p_IsActive
    WHERE Id = p_Id
      AND IsDeleted = 0;

    SELECT *
    FROM Spendwises
    WHERE Id = p_Id
      AND IsDeleted = 0;
END $$

CREATE PROCEDURE SP_GetAllSpendwises()
BEGIN
    SELECT *
    FROM Spendwises
    WHERE IsDeleted = 0
    ORDER BY CreatedOn DESC;
END $$

CREATE PROCEDURE SP_GetSpendwiseById(
    IN p_Id CHAR(36)
)
BEGIN
    SELECT *
    FROM Spendwises
    WHERE Id = p_Id
      AND IsDeleted = 0
    LIMIT 1;
END $$

CREATE PROCEDURE SP_DeleteSpendwise(
    IN p_Id CHAR(36),
    IN p_ModifiedOn DATETIME,
    IN p_ModifiedBy NVARCHAR(30)
)
BEGIN
    UPDATE Spendwises
    SET
        IsDeleted = 1,
        IsActive = 0,
        ModifiedOn = p_ModifiedOn,
        ModifiedBy = p_ModifiedBy
    WHERE Id = p_Id
      AND IsDeleted = 0;

    SELECT ROW_COUNT() AS AffectedRows;
END $$

DELIMITER ;

