using FluentAssertions;
using NekoT.Desktop.ViewModels;
using Xunit;

namespace NekoT.Tests.Desktop;

public class ChatViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyMessages()
    {
        var viewModel = new ChatViewModel();

        viewModel.Messages.Should().NotBeNull();
        viewModel.Messages.Should().BeEmpty();
    }

    [Fact]
    public void InputText_ShouldBeEmptyInitially()
    {
        var viewModel = new ChatViewModel();

        viewModel.InputText.Should().BeEmpty();
    }

    [Fact]
    public void IsSending_ShouldBeFalseInitially()
    {
        var viewModel = new ChatViewModel();

        viewModel.IsSending.Should().BeFalse();
    }

    [Fact]
    public void SendMessage_ShouldAddUserMessage()
    {
        var viewModel = new ChatViewModel();
        viewModel.InputText = "Hello, AI!";

        viewModel.SendMessage();

        viewModel.Messages.Should().HaveCount(1);
        viewModel.Messages[0].Role.Should().Be("user");
        viewModel.Messages[0].Content.Should().Be("Hello, AI!");
        viewModel.InputText.Should().BeEmpty();
    }

    [Fact]
    public void SendMessage_WithEmptyInput_ShouldNotAddMessage()
    {
        var viewModel = new ChatViewModel();
        viewModel.InputText = "   ";

        viewModel.SendMessage();

        viewModel.Messages.Should().BeEmpty();
    }

    [Fact]
    public void AddAssistantMessage_ShouldAddMessageWithCorrectRole()
    {
        var viewModel = new ChatViewModel();

        viewModel.AddAssistantMessage("Hello, user!");

        viewModel.Messages.Should().HaveCount(1);
        viewModel.Messages[0].Role.Should().Be("assistant");
        viewModel.Messages[0].Content.Should().Be("Hello, user!");
    }

    [Fact]
    public void ClearMessages_ShouldRemoveAllMessages()
    {
        var viewModel = new ChatViewModel();
        viewModel.AddAssistantMessage("Test");
        viewModel.Messages.Should().HaveCount(1);

        viewModel.ClearMessages();

        viewModel.Messages.Should().BeEmpty();
    }
}

public class ChatMessageTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var message = new ChatMessage("user", "Hello");

        message.Role.Should().Be("user");
        message.Content.Should().Be("Hello");
        message.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsUser_ShouldReturnTrueForUserRole()
    {
        var message = new ChatMessage("user", "Test");

        message.IsUser.Should().BeTrue();
    }

    [Fact]
    public void IsUser_ShouldReturnFalseForAssistantRole()
    {
        var message = new ChatMessage("assistant", "Test");

        message.IsUser.Should().BeFalse();
    }
}

public class SidePanelViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        var viewModel = new SidePanelViewModel();

        viewModel.IsOpen.Should().BeFalse();
        viewModel.ApiKey.Should().BeEmpty();
        viewModel.SelectedProvider.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TogglePanel_ShouldToggleIsOpen()
    {
        var viewModel = new SidePanelViewModel();
        var initialState = viewModel.IsOpen;

        viewModel.TogglePanel();

        viewModel.IsOpen.Should().Be(!initialState);
    }

    [Fact]
    public void TogglePanel_CalledTwice_ShouldReturnToInitialState()
    {
        var viewModel = new SidePanelViewModel();
        var initialState = viewModel.IsOpen;

        viewModel.TogglePanel();
        viewModel.TogglePanel();

        viewModel.IsOpen.Should().Be(initialState);
    }

    [Fact]
    public void SaveApiKey_ShouldStoreKey()
    {
        var viewModel = new SidePanelViewModel();
        viewModel.ApiKey = "placeholder-key";

        viewModel.SaveApiKey();

        viewModel.HasApiKey.Should().BeTrue();
    }

    [Fact]
    public void ClearApiKey_ShouldRemoveKey()
    {
        var viewModel = new SidePanelViewModel();
        viewModel.ApiKey = "placeholder-key";
        viewModel.SaveApiKey();

        viewModel.ClearApiKey();

        viewModel.HasApiKey.Should().BeFalse();
        viewModel.ApiKey.Should().BeEmpty();
    }
}