Console.WriteLine("Bootstrapper");

Console.Write("Enter Deployment Environment (DEV, QA, PROD)");
string? inputEnv = Console.ReadLine();

// if the input is null or empty default to "DEV" then convert to uppercase
string environment = string.IsNullOrWhiteSpace(inputEnv) ? "DEV" : inputEnv.ToUpper();

string dbConnectionString = environment switch
{
    "DEV" => "Server=localhost;Database=DevDb;Trusted_Connection=True;",
    "QA" => "Server=qa-db.intranet;Database=QaDb;Trusted_Connection=True;",
    "PROD" => "Server=prod-cluster;Database=ProdDb;Encrypt=True;",
    _ => "Server=localhost;Database=FallbackDb;Warning=UnknownEnv;" // The underscore is the default case
};

int port;
Console.Write("Enter Service Port (e.g., 8080): ");

// The loop keeps running AS LONG AS TryParse returns false.
while (!int.TryParse(Console.ReadLine(), out port))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Invalid input. Port must be a numeric value.");
    Console.ResetColor();
    Console.Write("Enter Service Port: ");
}

string logLevel = (environment == "PROD") ? "ERROR_ONLY" : "DEBUG_ALL";

Console.WriteLine("\n BOOTSTRAP COMPLETE ");
Console.WriteLine($"Environment : {environment}");
Console.WriteLine($"DB Connect  : {dbConnectionString}");
Console.WriteLine($"Port bound  : {port}");
Console.WriteLine($"Log Level   : {logLevel}");
Console.WriteLine("System is ready to accept requests...");