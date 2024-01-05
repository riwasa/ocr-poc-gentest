# ocr-poc-gentest
Generates an Excel file containing processed data test results.

## Requirements

Visual Studio 2022
.NET 6.0

## Instructions

1. In the Azure portal, open the Cosmos DB resource.
2. Go to the Settings / Keys blade.
3. Click the eye icon next to the Primary Connection String field.
4. Click the copy button in the field to copy the connection string.
5. Open the solution file in Visual Studio 2022.
6. In Program.cs, replace the entire value of the connectionString variable (everything between the quotation marks) on line 9 with the value you copied from the Azure portal.
  - string connectionString = "AccountEndpoint=https://<cosmos-db-account-name>.documents.azure.com:443/;AccountKey=<cosmos-db-account-key>;";
7. Run the program to extract data from the Cosmos DB database and create a CSV file with the test results. This file will be located in the bin/Debug/net6.0 file and will be named "YYYY-MM-dd-HH-mm-ss.csv". Every time the program is run a new file will be generated.

