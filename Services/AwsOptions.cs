namespace AqlaAwsS3Manager.Services;

public class AwsOptions
{
    public const string SectionName = "AWS";

    public string Region { get; set; } = "us-east-1";
    public string DefaultBucket { get; set; } = string.Empty;
}
