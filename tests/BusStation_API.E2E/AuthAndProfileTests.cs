using System.Net;
using System.Net.Http.Json;
using BusStation_API.E2E.Infrastructure;

namespace BusStation_API.E2E
{
    /// <summary>Cadastro, login e área "minha conta" (/user/me).</summary>
    [Collection(E2ECollection.Name)]
    public class AuthAndProfileTests
    {
        private readonly TestApi _api;

        public AuthAndProfileTests(BusStationApiFactory factory)
        {
            _api = new TestApi(factory);
        }

        [Theory]
        [InlineData("", "valid@e2e.test", "secret123", 30, "Name is required.")]
        [InlineData("Ana", "sem-arroba", "secret123", 30, "A Valid email is required")]
        [InlineData("Ana", "valid@e2e.test", "123", 30, "Password must be at least 6 characters long.")]
        [InlineData("Ana", "valid@e2e.test", "secret123", 17, "You must be an adult bro")]
        public async Task Register_rejects_invalid_input_with_a_message(string name, string email, string password, int age, string expected)
        {
            var response = await _api.Anonymous().PostAsJsonAsync("/register", new { name, email, password, age });

            await Expect.Status(response, HttpStatusCode.BadRequest);
            Assert.Equal(expected, (await response.Content.ReadFromJsonAsync<ErrorDto>())!.Message);
        }

        [Fact]
        public async Task Register_with_an_email_already_in_use_returns_409()
        {
            var (email, _) = await _api.RegisterUser();

            var response = await _api.Anonymous().PostAsJsonAsync("/register",
                new { name = "Clone", email, password = "secret123", age = 30 });

            await Expect.Status(response, HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Login_with_wrong_password_or_unknown_email_returns_401()
        {
            var (email, _) = await _api.RegisterUser();
            var client = _api.Anonymous();

            var wrongPassword = await client.PostAsJsonAsync("/login", new { email, password = "wrong-pass" });
            var unknownEmail = await client.PostAsJsonAsync("/login", new { email = "ghost@e2e.test", password = "secret123" });

            await Expect.Status(wrongPassword, HttpStatusCode.Unauthorized);
            await Expect.Status(unknownEmail, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Profile_requires_a_valid_user_token()
        {
            var anonymous = await _api.Anonymous().GetAsync("/user/me");
            var forged = await _api.WithToken("not-a-jwt").GetAsync("/user/me");

            await Expect.Status(anonymous, HttpStatusCode.Unauthorized);
            await Expect.Status(forged, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Admin_token_does_not_open_the_customer_area()
        {
            var admin = await _api.NewAdmin();

            var response = await admin.GetAsync("/user/me");

            await Expect.Status(response, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logged_user_sees_their_own_profile()
        {
            var (email, password) = await _api.RegisterUser(age: 42);
            var client = _api.WithToken(await _api.Login(email, password));

            var profile = await client.GetFromJsonAsync<UserProfileDto>("/user/me");

            Assert.Equal(email, profile!.Email);
            Assert.Equal("E2E User", profile.Name);
            Assert.Equal(42, profile.Age);
        }

        [Fact]
        public async Task Changing_password_invalidates_the_old_one_for_login()
        {
            var (email, password) = await _api.RegisterUser();
            var client = _api.WithToken(await _api.Login(email, password));

            var wrongCurrent = await client.PutAsJsonAsync("/user/me/password", new { password = "nope-nope", newPassword = "brandnew1" });
            await Expect.Status(wrongCurrent, HttpStatusCode.BadRequest);

            var tooShort = await client.PutAsJsonAsync("/user/me/password", new { password, newPassword = "123" });
            await Expect.Status(tooShort, HttpStatusCode.BadRequest);

            var changed = await client.PutAsJsonAsync("/user/me/password", new { password, newPassword = "brandnew1" });
            await Expect.Status(changed, HttpStatusCode.NoContent);

            var oldLogin = await _api.Anonymous().PostAsJsonAsync("/login", new { email, password });
            await Expect.Status(oldLogin, HttpStatusCode.Unauthorized);
            Assert.False(string.IsNullOrEmpty(await _api.Login(email, "brandnew1")));
        }

        [Fact]
        public async Task Changing_email_requires_password_and_a_free_address()
        {
            var (takenEmail, _) = await _api.RegisterUser();
            var (email, password) = await _api.RegisterUser();
            var client = _api.WithToken(await _api.Login(email, password));
            var newEmail = $"{TestApi.Unique("moved")}@e2e.test";

            var wrongPassword = await client.PutAsJsonAsync("/user/me/email", new { email = newEmail, password = "wrong-pass" });
            await Expect.Status(wrongPassword, HttpStatusCode.BadRequest);

            var taken = await client.PutAsJsonAsync("/user/me/email", new { email = takenEmail, password });
            await Expect.Status(taken, HttpStatusCode.BadRequest);

            var changed = await client.PutAsJsonAsync("/user/me/email", new { email = newEmail, password });
            await Expect.Status(changed, HttpStatusCode.NoContent);

            Assert.Equal(newEmail, (await client.GetFromJsonAsync<UserProfileDto>("/user/me"))!.Email);
            Assert.False(string.IsNullOrEmpty(await _api.Login(newEmail, password)));
        }

        [Fact]
        public async Task Admin_login_accepts_the_right_password_and_rejects_a_wrong_one()
        {
            var (email, password) = await _api.CreateAdminAccount();
            var client = _api.Anonymous();

            var right = await client.PostAsJsonAsync("/admin/login", new { email, password });
            var wrong = await client.PostAsJsonAsync("/admin/login", new { email, password = "wrong-pass" });
            var unknown = await client.PostAsJsonAsync("/admin/login", new { email = "ghost@e2e.test", password });

            await Expect.Status(right, HttpStatusCode.OK);
            Assert.False(string.IsNullOrEmpty((await right.Content.ReadFromJsonAsync<TokenResponse>())!.Token));
            await Expect.Status(wrong, HttpStatusCode.Unauthorized);
            await Expect.Status(unknown, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Admin_create_requires_an_admin_token()
        {
            var body = new { name = "Intruso", email = $"{TestApi.Unique("admin")}@e2e.test", password = "admin123" };

            var anonymous = await _api.Anonymous().PostAsJsonAsync("/admin/create", body);
            await Expect.Status(anonymous, HttpStatusCode.Unauthorized);

            var admin = await _api.NewAdmin();
            var created = await admin.PostAsJsonAsync("/admin/create", body);
            await Expect.Status(created, HttpStatusCode.Created);

            // o admin criado por outro admin consegue logar
            Assert.False(string.IsNullOrEmpty(await _api.AdminLogin(body.email, body.password)));
        }
    }
}
