import os
import subprocess
import shutil
import logging
import argparse

def setup_logging(verbose=False):
    log_format = '%(asctime)s - %(levelname)s - %(message)s'
    logging.basicConfig(level=logging.INFO, format=log_format)
    if verbose:
        logging.getLogger().setLevel(logging.DEBUG)

def check_github_ssh_access():
    try:
        result = subprocess.run(
            ['ssh', '-T', 'git@github.com'],
            capture_output=True,
            text=True,
            timeout=10
        )
        auth_success = 'successfully authenticated' in result.stderr.lower()
        if not auth_success:
            logging.error('GitHub SSH access failed: %s', result.stderr.strip())
        return auth_success
    except subprocess.TimeoutExpired:
        logging.error('Timeout trying to reach GitHub via SSH')
        return False
    except Exception as e:
        logging.error('Unexpected error during SSH check: %s', str(e))
        return False

def clone_repositories():
    logging.info('Cloning repositories...')

    if not os.path.isfile('repositories.txt'):
        logging.error('Error: repositories.txt not found')
        return False

    os.makedirs('repositories', exist_ok=True)

    success = True
    with open('repositories.txt') as f:
        for line in f:
            if not line.strip() or line.strip().startswith('#'):
                continue
            repo_url = line.split('#')[0].strip()
            if not repo_url:
                continue

            # Extract repository name
            logging.info('Cloning repository %s', repo_url)
            repo_name = os.path.splitext(repo_url.split('/')[-1])[0]
            target_dir = os.path.join('repositories', repo_name)

            try:
                subprocess.run(
                    ['git', 'clone', repo_url, target_dir],
                    check=True,
                    capture_output=True,
                    text=True
                )
            except subprocess.CalledProcessError as e:
                logging.error(f'Error cloning {repo_url}: {e.stderr}')
                success = False

    return success


def purge_repositories():
    logging.info('Purging repositories...')
    
    if not os.path.isdir('repositories'):
        logging.error('Error: repositories directory not found')
        return False
    
    try:
        shutil.rmtree('repositories')
        return True
    except Exception as e:
        logging.error(f'Error purging repositories: {e}')
        return False


def pull_repositories():
    logging.info('Pulling repositories...')

    if not os.path.isdir('repositories'):
        logging.error('Error: repositories directory not found')
        return False

    success = True
    for repo_name in os.listdir('repositories'):
        logging.info('Pulling repository %s', repo_name)
        repo_path = os.path.join('repositories', repo_name)
        if not os.path.isdir(repo_path):
            logging.error('Error: repository %s not found', repo_name)
            continue

        try:
            subprocess.run(
                ['git', 'pull'],
                cwd=repo_path,
                check=True,
                capture_output=True,
                text=True
            )
        except subprocess.CalledProcessError as e:
            logging.error(f'Error pulling {repo_name}: {e.stderr}')
            success = False

    return success


def switch_all_repositories_to_main():
    logging.info('Switching all repositories to main...')

    if not os.path.isdir('repositories'):
        logging.error('Error: repositories directory not found')
        return False

    success = True
    for repo_name in os.listdir('repositories'):
        logging.info('Switching repository %s', repo_name)
        repo_path = os.path.join('repositories', repo_name)
        if not os.path.isdir(repo_path):
            logging.error('Error: repository %s not found', repo_name)
            continue

        try:
            try:
                subprocess.run(
                    ['git', 'checkout', 'main'],
                    cwd=repo_path,
                    check=True,
                    capture_output=True,
                    text=True
                )
            except subprocess.CalledProcessError:
                subprocess.run(
                    ['git', 'checkout', 'master'],
                    cwd=repo_path,
                    check=True,
                    capture_output=True,
                    text=True
                )
        except subprocess.CalledProcessError as e:
            logging.error(f'Error switching {repo_name}: {e.stderr}')
            success = False

    return success


def main():
    parser = argparse.ArgumentParser(description='Helper tool for managing Anvil repositories')
    parser.add_argument('-v', '--verbose', action='store_true', help='Enable verbose logging')
    
    subparsers = parser.add_subparsers(dest='command', help='Commands')
    subparsers.required = True
    
    subparsers.add_parser('clone', help='Clone repositories')
    subparsers.add_parser('pull', help='Pull repositories')
    subparsers.add_parser('purge', help='Purge repositories')
    subparsers.add_parser('switch', help='Switch all repositories to main')
    
    args = parser.parse_args()
    
    setup_logging(args.verbose)
    logging.debug(f'Arguments: {args}')
    check_github_ssh_access()
    
    command_funcs = {
        'clone': clone_repositories,
        'pull': pull_repositories,
        'purge': purge_repositories,
        'switch': switch_all_repositories_to_main
    }
    
    success = command_funcs[args.command]()
    return 0 if success else 1

if __name__ == '__main__':
    exit(main())