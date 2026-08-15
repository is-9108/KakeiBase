# syntax=docker/dockerfile:1

# ============================================================
# Stage 1: build — アプリ本体を publish する
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# csproj だけ先にコピーして restore 結果をレイヤーキャッシュに載せる
COPY src/KakeiBase.WebApi/KakeiBase.WebApi.csproj src/KakeiBase.WebApi/
RUN dotnet restore src/KakeiBase.WebApi/KakeiBase.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/KakeiBase.WebApi/KakeiBase.WebApi.csproj \
      --no-restore -c Release -o /app/publish /p:UseAppHost=false

# ============================================================
# Stage 2: efbundle — EF Core マイグレーションバンドルを生成する
#
# ADR-0008 決定6 のとおり、マイグレーションは踏み台を置かず
# デプロイパイプラインから ECS の one-off タスクとして実行する。
# その実体がこの単一実行ファイル。
# --self-contained により .NET ランタイムのバージョン差異に依存しない。
# ============================================================
FROM build AS efbundle
ENV PATH="$PATH:/root/.dotnet/tools"
# ツールのバージョンは csproj の Microsoft.EntityFrameworkCore.Design に合わせる
RUN dotnet tool install --global dotnet-ef --version 10.0.*
RUN dotnet ef migrations bundle \
      --project         src/KakeiBase.WebApi/KakeiBase.WebApi.csproj \
      --startup-project src/KakeiBase.WebApi/KakeiBase.WebApi.csproj \
      --configuration Release \
      --self-contained -r linux-x64 \
      --force \
      -o /app/efbundle

# ============================================================
# Stage 3: runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# ADR-0008 決定6: 踏み台 EC2 を置かず ECS Exec でアドホックな DB 調査を行うため、
# psql を同梱する。この要件があるため chiseled イメージは採用していない。
RUN apt-get update \
 && apt-get install -y --no-install-recommends postgresql-client \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build    /app/publish  ./
COPY --from=efbundle /app/efbundle /app/efbundle
RUN chmod 0755 /app/efbundle

# .NET 8 以降の公式イメージが用意する非 root ユーザー
USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# ENTRYPOINT ではなく CMD にする。
# ECS の containerOverrides は command (= CMD) しか上書きできず entryPoint は上書きできない。
# ENTRYPOINT を置くとマイグレーション実行時に
#   dotnet KakeiBase.WebApi.dll /app/efbundle --verbose
# という「引数の追記」になり、Web API が起動してタスクが終了しなくなる。
CMD ["dotnet", "KakeiBase.WebApi.dll"]
