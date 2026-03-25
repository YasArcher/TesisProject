cd /d C:\Users\Personal\Source\Repos\TesisProject
start "backend-loadtest" /b cmd /c dotnet run --project C:\Users\Personal\Source\Repos\TesisProject\tesisproject.backend\tesisproject.backend.csproj 1^> C:\Users\Personal\Source\Repos\TesisProject\tests\load\backend-loadtest.out.log 2^> C:\Users\Personal\Source\Repos\TesisProject\tests\load\backend-loadtest.err.log
