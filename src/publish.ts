#!/usr/bin/env bun

import { $ } from "bun";
import { parseStringPromise } from "xml2js";

// Configuration
const NUGET_SOURCE = "https://api.nuget.org/v3/index.json";
const NUGET_API_KEY = process.env.NUGET_API_KEY;

const projects = {
  gnissel: "./Cooke.Gnissel/Cooke.Gnissel.csproj",
  npgsql: "./Cooke.Gnissel.Npgsql/Cooke.Gnissel.Npgsql.csproj",
};

// Check API key
if (!NUGET_API_KEY) {
  console.error("❌ Error: NUGET_API_KEY environment variable not set");
  process.exit(1);
}

// Read version from csproj file
async function getVersion(projectPath: string): Promise<string> {
  const file = Bun.file(projectPath);
  const content = await file.text();
  const parsed = await parseStringPromise(content);
  const version = parsed.Project.PropertyGroup.find((pg: any) => pg.Version)?.[
    "Version"
  ][0];

  if (!version) {
    throw new Error(`Version not found in ${projectPath}`);
  }

  return version;
}

// Main publish function
async function publish() {
  try {
    // Read versions
    console.log("📖 Reading versions from project files...");
    const gnisselVersion = await getVersion(projects.gnissel);
    const npgsqlVersion = await getVersion(projects.npgsql);

    console.log(`✅ Cooke.Gnissel version: ${gnisselVersion}`);
    console.log(`✅ Cooke.Gnissel.Npgsql version: ${npgsqlVersion}`);
    console.log();

    // Build projects
    console.log("🔨 Building Cooke.Gnissel...");
    await $`dotnet build -c Release ${projects.gnissel}`;

    console.log("🔨 Building Cooke.Gnissel.Npgsql...");
    await $`dotnet build -c Release ${projects.npgsql}`;

    console.log();

    // Push packages to NuGet
    console.log("📦 Publishing Cooke.Gnissel to NuGet.org...");
    await $`dotnet nuget push ./Cooke.Gnissel/bin/Release/Cooke.Gnissel.${gnisselVersion}.nupkg -k ${NUGET_API_KEY} -s ${NUGET_SOURCE}`;

    console.log("📦 Publishing Cooke.Gnissel.Npgsql to NuGet.org...");
    await $`dotnet nuget push ./Cooke.Gnissel.Npgsql/bin/Release/Cooke.Gnissel.Npgsql.${npgsqlVersion}.nupkg -k ${NUGET_API_KEY} -s ${NUGET_SOURCE}`;

    console.log();
    console.log(
      `✨ Successfully published Cooke.Gnissel ${gnisselVersion} and Cooke.Gnissel.Npgsql ${npgsqlVersion} to NuGet.org`
    );
  } catch (error) {
    console.error("❌ Publish failed:", error);
    process.exit(1);
  }
}

// Run
publish();
