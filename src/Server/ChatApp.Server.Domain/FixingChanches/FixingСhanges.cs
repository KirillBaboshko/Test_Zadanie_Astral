using ChatApp.Server.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Server.FixingChanges;

public static class FixingСhanges
{
    public static async Task FixChangesAsync(IUserRepository userRepository, CancellationToken cancellationToken = default)
    {
        await userRepository.SaveAsync(cancellationToken);
    }
}
