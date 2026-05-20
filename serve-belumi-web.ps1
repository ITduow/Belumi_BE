$ErrorActionPreference = 'Stop'

$root = Join-Path $PSScriptRoot 'belumi_app\build\web'
$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add('http://localhost:5200/')
$listener.Start()
Write-Host "Belumi web is serving $root at http://localhost:5200/"

$mimeTypes = @{
  '.html' = 'text/html; charset=utf-8'
  '.js' = 'application/javascript; charset=utf-8'
  '.mjs' = 'application/javascript; charset=utf-8'
  '.css' = 'text/css; charset=utf-8'
  '.json' = 'application/json; charset=utf-8'
  '.png' = 'image/png'
  '.jpg' = 'image/jpeg'
  '.jpeg' = 'image/jpeg'
  '.svg' = 'image/svg+xml'
  '.ico' = 'image/x-icon'
  '.wasm' = 'application/wasm'
  '.otf' = 'font/otf'
  '.ttf' = 'font/ttf'
}

while ($listener.IsListening) {
  $context = $listener.GetContext()
  try {
    $requestPath = [Uri]::UnescapeDataString($context.Request.Url.AbsolutePath.TrimStart('/'))
    if ([string]::IsNullOrWhiteSpace($requestPath)) {
      $requestPath = 'index.html'
    }

    $candidate = Join-Path $root $requestPath
    $fullPath = [System.IO.Path]::GetFullPath($candidate)
    $rootPath = [System.IO.Path]::GetFullPath($root)
    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase) -or -not [System.IO.File]::Exists($fullPath)) {
      $fullPath = Join-Path $root 'index.html'
    }

    $bytes = [System.IO.File]::ReadAllBytes($fullPath)
    $extension = [System.IO.Path]::GetExtension($fullPath).ToLowerInvariant()
    $context.Response.ContentType = $mimeTypes[$extension]
    if ([string]::IsNullOrWhiteSpace($context.Response.ContentType)) {
      $context.Response.ContentType = 'application/octet-stream'
    }
    $context.Response.StatusCode = 200
    $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
  }
  catch {
    $message = [System.Text.Encoding]::UTF8.GetBytes($_.Exception.Message)
    $context.Response.StatusCode = 500
    $context.Response.OutputStream.Write($message, 0, $message.Length)
  }
  finally {
    $context.Response.OutputStream.Close()
  }
}
