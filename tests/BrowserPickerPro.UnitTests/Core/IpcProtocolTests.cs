// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: IpcProtocolTests.cs
// Description: Wire-format tests for the launcher ↔ resident-app pipe protocol.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Ipc;
using FluentAssertions;
using Xunit;

namespace BrowserPickerPro.UnitTests.Core;

public class IpcProtocolTests
{
    [Fact]
    public void Open_message_round_trips()
    {
        var msg = new IpcMessage(IpcProtocol.CommandOpen, 123456789, "https://example.com/?q=a b");
        IpcProtocol.Parse(IpcProtocol.Format(msg)).Should().Be(msg);
    }

    [Fact]
    public void Tab_inside_the_argument_does_not_shift_fields()
    {
        var parsed = IpcProtocol.Parse("OPEN\t5\thttps://a.test/\tx");
        parsed!.Argument.Should().Be("https://a.test/\tx");
        parsed.Timestamp.Should().Be(5);
    }

    [Fact]
    public void Line_breaks_are_stripped_so_no_second_message_can_be_injected()
        => IpcProtocol.Format(new IpcMessage(IpcProtocol.CommandOpen, 1, "https://a.test/\r\nSETTINGS\t0\t"))
            .Should().NotContain("\n").And.NotContain("\r");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("OPEN")]
    [InlineData("OPEN\tnot-a-number\thttps://a.test")]
    [InlineData("DELETE\t1\tC:\\")]
    public void Malformed_or_unknown_lines_are_rejected(string? line)
        => IpcProtocol.Parse(line).Should().BeNull();

    [Fact]
    public void Names_are_scoped_to_the_user()
    {
        IpcProtocol.MutexName.Should().StartWith(@"Local\").And.Contain(Environment.UserName);
        IpcProtocol.PipeName.Should().Contain(Environment.UserName);
    }

    // Private pipe name: the default one would reach a real resident app on a dev machine
    // (it did once, popping a picker up on the screen).
    [Fact]
    public void Sending_without_a_server_fails_fast_instead_of_throwing()
        => IpcProtocol.TrySend(new IpcMessage(IpcProtocol.CommandOpen, 0, "https://a.test"), 50,
                               $"BrowserPickerPro.Test.{Guid.NewGuid():N}").Should().BeFalse();
}
