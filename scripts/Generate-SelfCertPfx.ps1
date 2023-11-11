param(
    [string]$Destination = "Watermarker_SelfSigned.pfx",
    [string]$DestinationBase64 = "Watermarker_SelfSigned.base64.txt"
)

$CertFriendlyName = "Watermarker_SelfSigned"
$CertPublisher = "CN=Encamy"
$CertStoreLocation = "Cert:\CurrentUser\My"

# Generate self signed cert
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $CertPublisher `
    -KeyUsage DigitalSignature `
    -FriendlyName $CertFriendlyName `
    -CertStoreLocation $CertStoreLocation `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

# Get size of the self signed cert
$certificateBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12)

# Save the self signed cert as a file
[System.IO.File]::WriteAllBytes($Destination, $certificateBytes)

# Generate base64 of cert
[System.Convert]::ToBase64String($certificateBytes) | Out-File $DestinationBase64
