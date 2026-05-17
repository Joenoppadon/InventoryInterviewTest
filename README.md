## Stack & Tools ##
    - Vs Code
    - Ms sql server
    - .Net core v.8
    - Bootstap
## package require .net core v.8 ##
    Microsoft.EntityFramworkCore --version 8.0
    Microsoft.EntityFramworkCore.Design --version 8.0
    Microsoft.EntityFramworkCore.SqlServer --version 8.0
## Set up Project ##
    1.add require package in terminal example 'dotnet add package Microsoft.EntityFramworkCore --version 8.0'
    2.Terminal --> 'dotnet tool install --global dotnet-ef --version 8.0'
    2.Change connection string 'condb' in appsetting.json
    3.Migration db terminal --> 'dotnet ef migrations add migrationname'
    4.Terminal --> 'dotnet ef database update'
    *if error check connection string and check installed require package
    
## Database ER Diagram ##

[PurchaseOrders]                     [Deliveries]
   - POID (PK)                          - DeliveryID (PK)
   - PONumber                           - DeliveryNumber
        |                                    |
        | (1)                                | (1)
        |                                    |
        +----------------+-------------------+
                         |
                         | (M)
                         v
               [StockTransactions]
                - TransID (PK)
                - RefID (FK to POID / DeliveryID) <--- RefType ('IN'/'OUT')
                - ProductId (FK) -------------------+
                - Qty                               |
                                                    | (M)
                                                    |
                                                    v
                                              [Products] (Master)
                                               - ProductId (PK)
                                               - ProductName
                                               - SKU
                                                    |
                                                    | (1)
                                                    |
                                                    v
                                              [Inventories] (Stock Balance)
                                               - InventoryID (PK)
                                               - ProductId (FK)
                                               - Quantity
 
Input Flow --> Add Product --> Receive --> Delivery 

