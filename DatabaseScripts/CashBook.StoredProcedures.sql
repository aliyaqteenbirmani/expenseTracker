DROP PROCEDURE IF EXISTS SP_AddCashBook;
DROP PROCEDURE IF EXISTS SP_UpdateCashBook;
DROP PROCEDURE IF EXISTS SP_GetAllCashBooks;
DROP PROCEDURE IF EXISTS SP_GetCashBookById;
DROP PROCEDURE IF EXISTS SP_DeleteCashBook;

DELIMITER $$

CREATE PROCEDURE SP_AddCashBook(
    IN p_Id CHAR(36),
    IN p_Name VARCHAR(255),
    IN p_CreatedOn DATETIME,
    IN p_CreatedBy NVARCHAR(200),
    IN p_ModifiedOn DATETIME,
    IN p_ModifiedBy NVARCHAR(30)
)
BEGIN
    INSERT INTO CashBooks
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
    FROM CashBooks
    WHERE Id = p_Id;
END $$

CREATE PROCEDURE SP_UpdateCashBook(
    IN p_Id CHAR(36),
    IN p_Name VARCHAR(255),
    IN p_ModifiedOn DATETIME,
    IN p_ModifiedBy NVARCHAR(30),
    IN p_IsActive BIT
)
BEGIN
    UPDATE CashBooks
    SET
        Name = p_Name,
        ModifiedOn = p_ModifiedOn,
        ModifiedBy = p_ModifiedBy,
        IsActive = p_IsActive
    WHERE Id = p_Id
      AND IsDeleted = 0;

    SELECT *
    FROM CashBooks
    WHERE Id = p_Id
      AND IsDeleted = 0;
END $$

CREATE PROCEDURE SP_GetAllCashBooks()
BEGIN
    SELECT *
    FROM CashBooks
    WHERE IsDeleted = 0
    ORDER BY CreatedOn DESC;
END $$

CREATE PROCEDURE SP_GetCashBookById(
    IN p_Id CHAR(36)
)
BEGIN
    SELECT *
    FROM CashBooks
    WHERE Id = p_Id
      AND IsDeleted = 0
    LIMIT 1;
END $$

CREATE PROCEDURE SP_DeleteCashBook(
    IN p_Id CHAR(36),
    IN p_ModifiedOn DATETIME,
    IN p_ModifiedBy NVARCHAR(30)
)
BEGIN
    UPDATE CashBooks
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
