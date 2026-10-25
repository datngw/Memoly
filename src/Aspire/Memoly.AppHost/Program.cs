var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").AddDatabase("memoly");
var seq = builder.AddSeq("seq");
var keycloak = builder.AddKeycloak("keycloak", 8080).WithRealmImport("./Realms");

builder.AddProject<Projects.Memoly_Api>("api")
    .WithReference(postgres)
    .WithReference(seq)
    .WithReference(keycloak)
    .WaitFor(postgres)
    .WaitFor(keycloak);

builder.Build().Run();
