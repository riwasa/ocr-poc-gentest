using System.Globalization;
using System.Text;
using System.Windows.Markup;

using Microsoft.Azure.Cosmos;

using Newtonsoft.Json.Linq;

string connectionString = "<add-cosmos-db-connection-string-here>";

string DatabaseName = "ocrPoc";
string PatientFormsContainerName = "forms";

CosmosClient cosmosClient = new CosmosClient(connectionString);

using FeedIterator<PatientForm> feedIterator = cosmosClient.GetContainer(DatabaseName, PatientFormsContainerName)
    .GetItemQueryIterator<PatientForm>("SELECT * FROM " + PatientFormsContainerName);

/*
 * 	• File Name: file_name
	• First Name: first_name
	• Last Name: last_name
	• Health Number: health_number
	• DOB: dob
	• Sex: sex
	• Primary Contact: primary_contact
	• Address: address
	• Ordering Physician Name: ordering_physician_name
	• Physician Address: physician_address
Tests Requested (comma-delimited list of test names): tests_requested
 */

// Write a semi-colon delimited list of the fields to a file.

// Create a file to write to.
using StreamWriter file = new($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.csv");

// Write a header line.
string header = "File Name, First Name, Last Name, Health Number, DOB, Sex, Primary Contact, Address, Ordering Physician Name, Physician Address, Tests Requested";
file.WriteLine(header);

int itemCount = 1;

while (feedIterator.HasMoreResults)
{
    FeedResponse<PatientForm> response = await feedIterator.ReadNextAsync();

    foreach (PatientForm patientForm in response)
    {
        string fileName = patientForm.BlobUrl.Split('/').Last().Replace(',', ';');
        string firstName = GetKeyValue(patientForm, "firstName");
        string lastName = GetKeyValue(patientForm, "lastName");
        string healthNumber = GetKeyValue(patientForm, "healthNumber");
        string dateOfBirth = GetKeyValue(patientForm, "dateOfBirth");
        string sex = GetSex(patientForm);
        string primaryContact = GetKeyValue(patientForm, "primaryContact");
        string address = GetKeyValue(patientForm, "address");
        string orderingPhysicianName = GetKeyValue(patientForm, "orderingPhysicianName");
        string physicianAddress = GetKeyValue(patientForm, "physicianAddress");
        string tests = GetTests(patientForm);
        string otherTests = GetTestsRequestedOnlySelected(patientForm);
        if (!string.IsNullOrWhiteSpace(otherTests))
        {
            tests = tests + "; " + otherTests;
        }

        string line = $"{fileName}, {firstName}, {lastName}, {healthNumber}, " +
            $"{dateOfBirth}, {sex}, {primaryContact}, {address}, " +
            $"{orderingPhysicianName}, {physicianAddress}, {tests}";

        Console.WriteLine($"Writing item {itemCount++}");

        file.WriteLine(line);
    }

    file.Close();
}


string GetKeyValue(PatientForm patientForm, string key)
{
    string value = "";

    if (patientForm.PatientForms != null)
    {
        foreach (PatientFormDocument document in patientForm.PatientForms)
        {
            value = document.Fields.Where(x => x.Key == key).Select(x => x.Value).FirstOrDefault();

            if (value != null)
            {
                break;
            }
        }
        
        if (value == null)
        {
            value = "";
        }
    }

    value = value.Replace(",", " ");

    return value;
}

string GetSex(PatientForm patientForm)
{
    string sex = "";

    if (patientForm.PatientForms != null)
    {
        foreach (PatientFormDocument document in patientForm.PatientForms)
        {
            if (document.Fields.Count > 0)
            {
                if (document.Fields.ContainsKey("sexM"))
                {
                    if (document.Fields["sexM"] == "True")
                    {
                        sex = "M";
                        break;
                    }
                }
                else if (document.Fields.ContainsKey("sexF"))
                {
                    if (document.Fields["sexF"] == "True")
                    {
                        sex = "F";
                        break;
                    }
                }
                else if (document.Fields.ContainsKey("sex"))
                {
                    sex = document.Fields["sex"];
                    break;
                }
            }
        }
    }

    return sex;
}

string GetTests(PatientForm patientForm)
{
    StringBuilder tests = new StringBuilder();

    if (patientForm.PatientForms != null)
    {
        foreach (PatientFormDocument document in patientForm.PatientForms)
        {
            foreach (string key in document.Fields.Keys)
            {
                switch (key)
                {
                    case "firstName":
                    case "lastName":
                    case "healthNumber":
                    case "dateOfBirth":
                    case "primaryContact":
                    case "address":
                    case "orderingPhysicianName":
                    case "physicianAddress":
                    case "customerName":
                    case "sex":
                        continue;
                    default:
                        string value = document.Fields[key];
                        if (value != null)
                        {
                            if (value == "False" || string.IsNullOrWhiteSpace(value) ||
                                key == "sexM" || key == "sexF")
                            {
                                // Ignore any selection marks marked as false, any
                                // fields that are empty, or male/female selection marks.
                                continue;
                            }
                            else if (value == "True" || value.Contains(":selected:") ||
                                key.Contains(":selected:"))
                            {
                                // For selection marks that are true, save the key (i.e. the test name).
                                value = key;
                            }

                            value = value.Replace(";", " ");
                            value = value.Replace(",", " ");
                            value = value.Replace("\n", "");
                            tests.Append($"{value}; ");
                        }
                        break;
                }
            }
        }
    }

    return tests.ToString();
}

string GetTestsRequestedOnlySelected(PatientForm patientForm)
{
    StringBuilder tests = new StringBuilder();

    if (patientForm.PatientForms != null)
    {
        foreach (PatientFormDocument document in patientForm.PatientForms)
        {
            foreach (string key in document.Tables.Keys)
            {
                if (key != "testsRequestedOnlySelected")
                {
                    continue;
                }

                List<Dictionary<string, string>> rows = document.Tables[key];

                foreach (Dictionary<string, string> row in rows)
                {
                    foreach (var column in row)
                    {
                        string value = column.Value;

                        if (!string.IsNullOrEmpty(value))
                        {
                            value = value.Replace(";", " ");
                            value = value.Replace(",", " ");
                            value = value.Replace("\n", "");
                            tests.Append($"{value}; ");
                        }
                    }
                }
            }
        }
    }

    return tests.ToString();
}


/// <summary>
/// Patient form class.
/// </summary>
internal class PatientForm
{
    /// <summary>
    /// URL of the blob containing the form.
    /// </summary>
    public string BlobUrl { get; set; }

    /// <summary>
    /// Id of the patient form. This id is auto-generated.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Form key/value pairs. Populated for pre-defined models.
    /// </summary>
    //public Dictionary<string, string> KeyValuePairs { get; set; } = new Dictionary<string, string>();
    public List<KeyValuePair<string, string>> KeyValuePairs { get; set; } = new List<KeyValuePair<string, string>>();

    /// <summary>
    /// The type of model used to analyze the form; for custom forms,
    /// the name of the custom model used.
    /// </summary>
    public string ModelTypeProposed { get; set; }

    /// <summary>
    /// The 1-based page number in the document where the form was found.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Patient form documents. Populated for custom models.
    /// </summary>
    public List<PatientFormDocument> PatientForms { get; set; } = new List<PatientFormDocument>();

    /// <summary>
    /// Status of the form processing.
    /// </summary>
    public string ProcessingStatus { get; set; }

    /// <summary>
    /// Form tables. Populated for pre-defined models.
    /// </summary>
    public List<List<string>> Tables { get; set; } = new List<List<string>>();

    /// <summary>
    /// Last update date/time.
    /// </summary>
    public DateTime UpdateDateTime { get; set; }
}

/// <summary>
/// Patient form document.
/// </summary>
internal class PatientFormDocument
{
    /// <summary>
    /// The custom fields in a document.
    /// </summary>
    public Dictionary<string, string> Fields { get; set; } = new();

    /// <summary>
    /// The type of model actually used to analyze the form, as returned in the 
    /// analyze response. For custom forms, this will be a colon-delimited string
    /// where both values are the same. For composite models, the first value will
    /// be the composite model name, and the second value will be the sub-model name.
    /// </summary>
    public string? ModelTypeActual { get; set; }

    /// <summary>
    /// The custom tables in a document. This is structured as a dictionary, where the
    /// key is the table name, and the value is a list of rows. Each row is a dictionary,
    /// where the key is the column name, and the value is the column value.
    /// </summary>
    public Dictionary<string, List<Dictionary<string, string>>> Tables { get; set; } = new();
}
