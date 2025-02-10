# Aspire

## Submodule Info

### Steps to init the modules

- Clone Anvil repository if not already
- navigate to root folder
- run `git submodule update --init --recurse` - this command should also be run whenever you pull in main repo.

### Updating subrepositories

- when working on either Universe or frontend, you should work in `Anvil/repositories/`. This way, when you commit / change branches & push changes in submodules,  you can easily update the submodule reference in the main repository
