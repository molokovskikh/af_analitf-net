# Add files and commands to this file, like the example:
#   watch(%r{file/path}) { `command(s)` }
#
guard 'shell' do
 watch(/.*/) {|m|
    dst = "../run"
    pidfile = "#{dst}/pidfile"
    exe = "#{dst}/AnalitF.Net.Client.exe"
    m[0] + " has changed."
    #`kill \`cat #{pidfile}\``
    #`rsync --exclude '*.log' -t ./* #{dst}`
    #`./#{exe}`
  }
end
