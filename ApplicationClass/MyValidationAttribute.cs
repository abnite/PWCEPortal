using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ApplicationClass;

public class MaxFileSizeAttribute:ValidationAttribute
{
    private readonly int _maxFileSize;
    
    public MaxFileSizeAttribute(int maxFileSize)
    {
        _maxFileSize = maxFileSize;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            if (file.Length > _maxFileSize)
            {
                return new ValidationResult(GetErrorMessage());
            }
        }

        return ValidationResult.Success;
    }

    public string GetErrorMessage()
    {
        return $"Maximum allowed file size is {_maxFileSize / 1024 / 1024}MB";
    }
}

public class CustomImageValidationAttribute : ValidationAttribute
{
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };
    private readonly string[] _allowedContentTypes = { "image/jpeg", "image/png" };

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            // Check extension
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return new ValidationResult(GetErrorMessage());
            }

            // Check content type
            if (!_allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return new ValidationResult(GetErrorMessage());
            }
        }

        return ValidationResult.Success;
    }

    public string GetErrorMessage()
    {
        return "Only .jpg, .jpeg, or .png files are allowed";
    }
}